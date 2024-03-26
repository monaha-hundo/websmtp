# websmtp
[websmtp-ui.webm](https://github.com/monaha-hundo/websmtp/assets/139830086/e3310bb1-e6fc-462f-871c-ed2f1c7ef788)
###### __Under development.__
## Description
Simple combined mail transfer agent and mail user agent with a web UI in C#, meaning the app receive emails from remote SMTP servers on port 25 and send mail directly to remote exchanges without going through relay servers. Purpose is to be simple to setup and use while being somewhat flexible. The app web interface is the sole consumer of the messages store.

Think of it as self-hosted replacement for email services like gmail, outlook and proton mail.

Spam handling is done through Apache's Spam Assassin (configurable on/off).

It is possible to display HTML emails with their embeded media content. Remote content is blocked by CSP. It is possible to completely disable HTML display (or only media content) to harden the web interface.

The app has a small command line utility to quicly generate a DKIM setup and outgoing emails can then be DKIM signed. It is also possible to import existing DKIM DNS setup in the app through manual configuration.

Possible uses: mock SMTP server for testing, home emails for LAN devices such as printers and ip cameras, your own web email provider business.



## Usage
### Configuration
The `appSettings.json` file is empty and contains all the configurable keys. All keys must be configured. Environment decide if `appSettings.Development.json` or `appSettings.Production.json` will be loaded additionaly. Each file must be present to run the app in each respective environment. 
The `Test` environment use the development configuration and _apply database migrations automatically_. Do not run in test environment if database modifications cannot be applied (e.g. not suitable for production database testing).

Available command line arguments:
- `--add-user`: quickly add a user by answering a couple of questions. User will have OTP disabled.
  - `--displayName=`
  - `--username=`
  - `--password=`
  - `--email=`
  - `--mailbox=`
  - `--roles=`
- `--list--users`: display all the users data in JSON.
- `--migrate-database`: apply migrations to database up to the lastest available at build time.
- `--enable-admin`: enable/create an admin user, similar to `--add-user` but if the username is taken, skip.
  - `--username=`
  - `--password=`
  - `--domain=`:
- `--generate-dkim-config`: generate the pub/priv certs, DNS record data and the configuration file data to enable DKIM signing and DNS configuration. Output contains information to configure a domain records.

### Running

#### Binary
By default the app is published as self-contained single file executable. 
Running the app once configured done by running the `websmtp` binary.

#### Docker
Available on the docker hub: [yvansolutions/websmtp](https://hub.docker.com/r/yvansolutions/websmtp).
You must generate all the HTTPS certificates and optionally the DKIM signing certificate and mount their location in the `/certificates/` volume.
You must then use the required environment variables to configure the app to use them (`SSL__PrivateKey, SSL__PublicKey, DKIM__Domains__X__PrivateKey`).
If you don't have certs on hand, use the provided `setup_docker_compose.sh` to quicly generate self-signed localhost ones for HTTPS and the `--generage-dkim-config` command line utility to get started with DKIM.

##### Building
Use the provided `build_docker.sh`
##### Launching
Assuming the certificates to be used are in the `/websmtp/certificates` folder.

Here is a command to run the app for the `example.com` domain:
`docker run -it -p 443:443 -p 25:25 -v /websmtp/certificates/:/certificates/:ro -e Database__Server=some-database-server yvansolutions/websmtp:latest ./websmtp --migrate-database --enable-admin --username=admin --password=admin --domain=example.com`.

This would launch the app, which would use `some-database-server` with the default database named `websmtp` with default credentials of `websmtp/websmtp`. It would listen for HTTPS connection on port `443` and SMTP `25`. An admin account with a catch-all mailbox and an identity of `postmaster@example.com`.

If DKIM setup was donne corretly, the app can send and receive email from major providers such as gmail, outlook and proton mail.

Here are the avaiable environment variables and their default:

`AllowedHosts: '*'`
`Database__Name: websmtp`
`Database__Password: websmtp`
`Database__Server: mariadb`
`Database__Username: websmtp`
`DKIM__Enabled: True`
`DKIM__SigningEnabled: False`
`DKIM__Domains__0__Name: websmtp.local`
`DKIM__Domains__0__PrivateKey: dkim_private.dev.pem`
`DKIM__Domains__0__Selector: dkim`
`DNS__IP: 192.168.1.1`
`DNS__Port: 53`
`Security__EnableHtmlDisplay: True`
`Security__EnableMediaInHtml: True`
`SMTP__Port: 25`
`SMTP__RemotePort: 25`
`SpamAssassin__Enabled: True`
`SPF__Enabled: True`
`SSL__Enabled: True`
`SSL__Port: 443`
`SSL__PrivateKey: /certificates/ssl.key`
`SSL__PublicKey: /certificates/ssl.crt`

#### Docker Compose
A `compose.yaml` file is available to quicly launch an instance without setting up a database/server. 

Use `docker compose up` in the root folder . 
Visit `https://localhost/`, use `admin/admin` as the default credentials. 
Visit `http://localhost:8080` for a adminer instance to connect to the `mariadb` instance/database. 
Adminer is the only way to manage users of a running instance.

### Testing
By default the `appSettings.Development.json` will look for a test/dev database on localhost, as such it is recommended to use docker and launch a local MariaDb instance for each test run.
Each test is responsible for creating a test user, logging in and cleaning up the test user.
The send mail test depends on running a local DNS server with mock domains and records (done programmatically, no external services are required), make sure to use ports bindable by the test host (e.g. > 1000).
**Let it be noted that many ISP, corporate firewall and small businesses block all traffic on port 25.**

### Concepts

#### Mailboxes
Mailboxes are "rules" which dictates to which users incoming emails will be delivered to. Wildcards (*) are used to create catch-all mailboxes. Users can have multiple mailboxes on multiple domains. 
#### Identity
Identities are email addresses and display name combinations offered to users as expeditor when sending emails. Users can only send email as identities for which they are assigned.

## Disclaimer
In active development, partial features, missing features, security issues, not tested, etc.

## Made with
- [ .Net 8 / Asp.Net / EntityFramework Core](https://dotnet.microsoft.com/)
- [Bootstrap](https://getbootstrap.com/)
- [MailKit](https://github.com/jstedfast/MailKit) & [MimeKit](https://github.com/jstedfast/MimeKit) 
- [MariaDb](https://mariadb.org/) & [Pomelo's MySQL Provider for .net](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql)
- [SweetAlert2](https://sweetalert2.github.io/)
- [Otp.Net](https://github.com/kspearrin/Otp.NET)
- [QRCoder](https://github.com/codebude/QRCoder)
- [SmtpServer](https://github.com/cosullivan/SmtpServer)
- [Apache's Spam Assassin](https://spamassassin.apache.org/)
