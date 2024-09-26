FROM cr.stackoverflow.software/so-pubplat-cnab:2024.9.25.214002-238.1@sha256:bd33fc10fb6f59e328d7210a6fa48cae470f5d25ce6da115f6c877e42f9204f3
    
COPY ./cnab/app /cnab/app
COPY ./charts /cnab/app/charts
RUN chmod 755 /cnab/app/run.ps1

CMD ["/cnab/app/run.ps1"]