FROM crdev.stackoverflow.software/so-pubplat-cnab:2024.9.25.171252-235.1-prerelease@sha256:27c8531f24c28602c88d1c3cad2306c27be97d38aa560e02d4151c38ce736803
    
COPY ./cnab/app /cnab/app
COPY ./charts /cnab/app/charts
RUN chmod 755 /cnab/app/run.ps1

CMD ["/cnab/app/run.ps1"]