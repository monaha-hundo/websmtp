# syntax=docker/dockerfile:1
FROM debian:bookworm

# filesystem
ADD ./build/ /
RUN mkdir -p /certificates/
VOLUME /certificates/

# apt dependencies
RUN apt update -y && apt upgrade -y
RUN apt install -y openssl spamassassin

ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

# networking
EXPOSE 25
EXPOSE 443

# launch
CMD ["./websmtp", "--migrate-database", "--enable-admin", "--username=admin", "--password=admin"]