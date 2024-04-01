# syntax=docker/dockerfile:1
FROM yvansolutions/spamassassin:latest

# filesystem
ADD ./build/ /
RUN mkdir -p /certificates/
VOLUME /certificates/

ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

# networking
EXPOSE 25
EXPOSE 5000

# launch
CMD ["./websmtp", "--migrate-database", "--enable-admin", "--username=admin", "--password=admin"]