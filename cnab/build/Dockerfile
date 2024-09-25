FROM cr.stackoverflow.software/so-pubplat-cnab:2024.9.25.172730-236.1@sha256:53f14d754c6595927b23eeaf143d154260f7da59b29faa6f39557e137624cc31
    
COPY ./cnab/app /cnab/app
COPY ./charts /cnab/app/charts
RUN chmod 755 /cnab/app/run.ps1

CMD ["/cnab/app/run.ps1"]