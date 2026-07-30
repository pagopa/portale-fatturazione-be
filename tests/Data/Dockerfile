FROM mcr.microsoft.com/mssql/server:2025-latest

ENV ACCEPT_EULA=Y
ENV SA_PASSWORD=52JdGnzZaANhf;

COPY ./Data /scripts


ENTRYPOINT [ "/bin/bash", "/scripts/entrypoint.sh" ]
CMD [ "/opt/mssql/bin/sqlservr" ]