//update in version repository
def update_version(environment, service, version) {
  def versions = [:]
  def versions_filename = "ohcmp-apps/versions-${environment}.yaml"
  lock("versions") {
    try {
      versions = readYaml file:versions_filename
    } catch (Exception ex) {
      //version file for this environment doesn't exists yet
      println ex
    }
    //update service version
    versions.remove(service)
    versions.put(service, ["image": ["tag": "${version}"]])
    writeYaml file:versions_filename, data:versions.sort(), overwrite: true
    //push to repository
    withCredentials([usernamePassword(credentialsId: 'github_ocs_jenkins', passwordVariable: 'GIT_PASSWORD', usernameVariable: 'GIT_USERNAME')]) {
      try {
        sh """
          printf \'#!/bin/bash\necho username=${GIT_USERNAME}\necho password=${GIT_PASSWORD}\' > credential-helper.sh
          git config credential.helper "/bin/bash ${env.WORKSPACE}/additional/infrastructure/credential-helper.sh"
          git add ${versions_filename}
          git config --global user.email "ocs-jenkins-github@monster.com"
          git config --global user.name "Jenkins"
          git commit -m "[CI] Version update"
          git push --set-upstream origin main
        """
      } catch (Exception ex) {
        println "Nothing to commit in to version repository. Skipping."
      }
    }
  }
}

def deploy(env, service, version) {
  update_version(env, service, version)
  echo "Deploy to ${env}"
}

// evaluate the body block, and collect configuration into the object
Map config = [:]

// required pipeline values
List requiredValues = [ ]

// the pipeline defaults
Map defaults = [
  // necessary parameters
  bundleDir:            'build',
  buildDir:             'src',
  // stage skip parameters
  skipDeploy: false,
  skipSonarScan:        true,
  skipSonarQualityGate: true,
  skipApply: false
]

// set the defaults on the pipeline if not already defined
defaults.each { key, defaultValue ->
  if (!config.containsKey(key)) { config[key] = defaultValue }
}

// set the pipeline
config.pipeline     = 'dockerPipeline'
config.pipelineType = 'iac'

///////////// Pipeline Definition /////////////
pipeline {
  agent any

  options {
    timestamps()
    ansiColor('xterm')
    timeout(time: config.timeout?.time ?: 5, unit: config.timeout?.unit ?: 'MINUTES')
  }

  triggers {
    issueCommentTrigger('^/(qa|dev)$')
  }

  parameters {
    booleanParam(name: 'DEPLOY_TO_PROD', defaultValue: true, description: 'Deploy to production if runs top of main branch')
    choice(name: 'DEPLOY_FORCE', choices: ['none', 'dev', 'qa', 'qax7', 'qax5', 'prod'], description: 'Force deploy this version to selected environment(s)')
  }

  stages {

    stage('SCM') { steps { script {
      github.addLabel(env, 'Building...')

      config.registry_id = 'monsternext-ocs-docker-prod-local.jfrog.io'
      config.group_id = 'io.monster.ocs'
      config.service_name = env.GIT_URL.replaceFirst(/^.*\/([^\/]+?).git$/, '$1')
      config.deploy_to = []
      config.all_envs = ['ams-dev', 'ams-qax5', 'ams-qax6', 'qax7', 'ams-staging', 'ams-prod']
      config.tf_vars = [:]
      config.zip_name = "${config.service_name}.zip"
      //TODO - generated for now
      config.version = "1.0.0.0-${currentBuild.startTimeInMillis}"
      config.zip_version_name = "${config.service_name}-${env.BUILD_TAG}.zip"
      config.repo_name = "ocs-binary-prod"

      //commit message to environment variable
      sh 'git log -1 --pretty=format:"%s" > gitcommitmsg.txt'
      config.gitcommitmsg = readFile(file: 'gitcommitmsg.txt')
      buildDescription config.gitcommitmsg

      //send welcome message to PR
      if (env.BUILD_ID == "1") {
        github.addComment(env, "Supported commands:\n/qa - Install to QAX12 and QAX6\n/dev - Install to DEV\n\nPlease wait with execute command until initial job will be done.")
      }

      def triggerCause = currentBuild.rawBuild.getCause(org.jenkinsci.plugins.pipeline.github.trigger.IssueCommentCause)

      if (triggerCause) {
        if (triggerCause.comment == '/qa') {
          config.deploy_to.addAll('ams-qax6', 'ams-staging')
          echo "This QA deploy has been triggered by comment"
        }

        if (triggerCause.comment == '/dev') {
          config.deploy_to.add('ams-dev')
          echo "This DEV deploy has been triggered by comment"
        }
      }

      //avoid of repeat deploy to DEV
      if (currentBuild.getBuildCauses('hudson.model.Cause$UserIdCause')) {
        gitcommitmsg = ''
        echo "Force deploy to environment: ${params.DEPLOY_FORCE}"
      }

      //Deploy environment decision
      switch(params.DEPLOY_FORCE) {
        case 'dev':
          config.deploy_to.add('ams-dev')
          break
        case 'qa':
          config.deploy_to.addAll('ams-qax6', 'ams-staging')
          break
        case 'qax7':
          config.deploy_to.add('qax7')
          break
        case 'qax5':
          config.deploy_to.add('ams-qax5')
          break
        case 'prod':
          config.deploy_to.add('ams-prod')
          break
      }

      if(config.deploy_to.isEmpty()) {
        //dev
        if(( (env.GIT_BRANCH != "main") && !(env.GIT_BRANCH =~ /^PR-/) ) && ( config.gitcommitmsg =~ /\/dev/)) {
          config.deploy_to.add('ams-dev')
        }
        //prod
        if( (env.GIT_BRANCH == "main") && (params.DEPLOY_TO_PROD)) {
          config.deploy_to.add('ams-prod')
        }
      }
      //--

      //permissions test: user is allow to run job manually to production
      if(config.deploy_to.contains('ams-prod')) {
        if (!auth.check_permissions(['Jenkins-MGS-OCS-DevOPS'])) {
          error("Current user hasn't permissions for deploy to production. Only members of DevOPS has.")
        }
      }
      //--
    }}}

    stage('Build') {
       steps { script {
        docker.build("${config.registry_id}/${config.group_id}/${config.service_name}:${config.version}", "--build-arg VERSION=${config.version} --no-cache .")
      }}

      post {
          success {
            script { github.updateStatus(env, true) }
          }
          failure {
            script { github.updateStatus(env, false) }
          }
      }
  }

    stage('Push to repository') {
        when {
          expression { return config.deploy_to.isEmpty() == false }
        }
        steps { script {
          docker.withRegistry("https://${config.registry_id}", "jenkins-jfrog") {
            docker.image("${config.registry_id}/${config.group_id}/${config.service_name}").push(config.version)
          }}

        }

        post {
            success {
              script {
                github.addLabel(env, 'Image:'+config.zip_version_name)
                github.addLabel(env, 'Version:'+config.version)
                github.updateStatus(env, true)
              }
            }
            failure {
              script { github.updateStatus(env, false) }
            }
        }
    }

    stage('Deploy') {
      when {
        expression { return config.deploy_to.isEmpty() == false }
      }
      steps { script {
        if(config.skipDeploy == false) {
          dir('additional/infrastructure') {
            git branch: 'main' , url: 'https://github.com/Monster-OCS/ohcmp-helm-charts.git', credentialsId: 'github_ocs_jenkins'
            for(env in config.all_envs) {
              stage("Deploy-${env}") {
                if(config.deploy_to.contains(env)) {
                  lock("ohcmp-deploy-${env}") {
                    config.tf_vars['image_id'] = config.zip_version_name
                    deploy(env, config.service_name, config.version )
                  }
                }else {
                  echo "Skipped deploy to ${env}"
                }
              }
            }
          }
        }else {
          echo "Deploy skipped due pipeline configuration: skipDeploy = true"
        }
      }}

    }


  }

  post {
    success {
      script {
        github.delLabel(env, 'Building...')
        github.addLabel(env, 'Build Passed')
        github.addComment(env, 'This build has passed all the steps')
      }
    }
    failure {
      script {
        github.delLabel(env, 'Building...')
        github.addLabel(env, 'Build Failed')
        github.addComment(env, 'Error in the CI/CD pipeline. See URL for a detail.' )
      }
    }
    //cleanup
    always {
      script {
        slack.sendStatus(config.service_name)
      }
    }
  }


}
