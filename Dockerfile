# syntax=docker/dockerfile:1
FROM yvansolutions/spamassassin:latest

RUN useradd -m websmtp 
RUN apt install -y libcap2-bin

# filesystem
ADD ./build/ /
RUN setcap 'cap_net_bind_service=+ep' ./websmtp

RUN mkdir -p /certificates/
RUN chown websmtp:websmtp /certificates/
VOLUME /certificates/

ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

# networking
EXPOSE 25
EXPOSE 5000

# launch
USER websmtp
CMD ["./websmtp", "--migrate-database"]