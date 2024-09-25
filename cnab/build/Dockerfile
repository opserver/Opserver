FROM crdev.stackoverflow.software/so-pubplat-cnab:2024.9.25.143139-prerelease@sha256:e59e59b4eb0d9bfcb6a9b404244184510fd92ae1cf62416c9bfcafadeefb6a4d
    
COPY ./cnab/app /cnab/app
COPY ./charts /cnab/app/charts
RUN chmod 755 /cnab/app/run.ps1

CMD ["/cnab/app/run.ps1"]