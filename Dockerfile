# syntax=docker/dockerfile:1
FROM debian:bookworm

ADD ./build/ /

RUN apt update -y && apt upgrade -y
RUN apt install -y openssl spamassassin

# generate websmtp.local self-signed certificates
RUN <<EOF
openssl req -x509 -newkey rsa:4096 -sha256 -days 3650 \
  -nodes -keyout websmtp.local.key -out websmtp.local.crt -subj "/CN=websmtp.local" \
  -addext "subjectAltName=DNS:websmtp.local,DNS:*.websmtp.local"
EOF

ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

# setup admin user
#RUN ["./websmtp", "--add-user", "--displayName=Postmaster", "--username=postmaster", "--password=postmaster", "--roles=admin", "--email=postmaster@websmtp.local", "--mailbox=*@*" ]

# final configuration
EXPOSE 25
EXPOSE 443
CMD ["./websmtp", "--migrate-database", "--enable-admin", "--username=admin", "--password=admin"]