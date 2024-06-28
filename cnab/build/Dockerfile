FROM mcr.microsoft.com/powershell

ARG Google_Cloud_Cli_Version=467.0.0-0
ARG Google_Cloud_Gke_Cloud_Auth_Plugin_Version=467.0.0-0
ARG KUBERNETES_VERSION=1.28
ARG HELM_VERSION=3

SHELL ["/bin/bash", "-o", "pipefail", "-c"]

RUN apt-get update \
    && apt-get install --no-install-recommends -y \ 
        ca-certificates \
        curl apt-transport-https \
        lsb-release\
        gnupg \
        jq \
        git \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Install kubectl
RUN curl -fsSL https://pkgs.k8s.io/core:/stable:/v${KUBERNETES_VERSION}/deb/Release.key | gpg --dearmor -o /etc/apt/keyrings/kubernetes-apt-keyring.gpg \
    && echo "deb [signed-by=/etc/apt/keyrings/kubernetes-apt-keyring.gpg] https://pkgs.k8s.io/core:/stable:/v${KUBERNETES_VERSION}/deb/ /" | tee /etc/apt/sources.list.d/kubernetes.list \
    && apt-get update \
    && apt-get install -y kubectl \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Install Helm
ENV HELM_URL=https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-${HELM_VERSION}
RUN curl -fsSL -o get_helm.sh ${HELM_URL} \
    && chmod 700 get_helm.sh \
    && ./get_helm.sh

# Install Helm diff
RUN helm plugin install https://github.com/databus23/helm-diff

# Install Google Cloud CLI
# https://cloud.google.com/sdk/docs/downloads-apt-get
RUN curl https://packages.cloud.google.com/apt/doc/apt-key.gpg | gpg --dearmor -o /usr/share/keyrings/cloud.google.gpg && \
    echo "deb [signed-by=/usr/share/keyrings/cloud.google.gpg] https://packages.cloud.google.com/apt cloud-sdk main" | tee -a /etc/apt/sources.list.d/google-cloud-sdk.list && \
    apt-get update && apt-get install -y google-cloud-sdk=${Google_Cloud_Cli_Version} google-cloud-cli=${Google_Cloud_Cli_Version} google-cloud-sdk-gke-gcloud-auth-plugin=${Google_Cloud_Gke_Cloud_Auth_Plugin_Version} && \
    rm -rf /var/lib/apt/lists/*
    
COPY ./cnab/app /cnab/app
COPY ./charts /cnab/app/charts
RUN chmod 755 /cnab/app/run.ps1

CMD ["/cnab/app/run.ps1"]