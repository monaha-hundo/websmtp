# syntax=docker/dockerfile:1
FROM debian:bookworm
ENV ASPNETCORE_ENVIRONMENT=Development

# setup mariadb
RUN apt-get update && apt-get install -y mariadb-server
COPY setup.sql /
RUN cat setup.sql | sudo mariadb

# add websmtp
ADD websmtp.tar.gz /

# generate websmtp.local self-signed certificates
RUN <<EOF
openssl req -x509 -newkey rsa:4096 -sha256 -days 3650 \
  -nodes -keyout websmtp.local.key -out websmtp.local.crt -subj "/CN=websmtp.local" \
  -addext "subjectAltName=DNS:websmtp.local,DNS:*.websmtp.local,IP:127.0.0.1"
EOF

# setup database
RUN ["websmtp","--migrate-database"]

# setup admin user
RUN ["websmtp", "--add-user", "--displayName=Postmaster", "--username=postmaster", "--password=postmaster", "--roles=admin", "--email=postmaster@websmtp.local", "--mailbox=*@*" ]

# final configuration
EXPOSE 1025
EXPOSE 1443
CMD ["websmtp"]