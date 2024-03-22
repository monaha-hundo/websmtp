# websmtp
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
- `--list--users`: display all the users data in JSON.
- `--migrate-database`: apply migrations to database up to the lastest available at build time.
- `--generate-dkim-config`: generate the pub/priv certs, DNS record data and the configuration file data to enable DKIM signing and DNS configuration. 

### Running

#### Binary
By default the app is published as an AOT single file executable. 
Running the app once configured done by running the `websmtp` binary.

#### Docker
Someday...

### Testing
By default the `appSettings.Development.json` will look for a test/dev database on localhost, as such it is recommended to use docker and launch a local MariaDb instance for each test run.
Each test is responsible for creating a test user, logging in and cleaning up the test user.
The send mail test depends on running a local DNS server with mock domains and records (done programmatically, no external services are required), make sure to use ports bindable by the test host (e.g. > 1080).
**Let it be noted that many ISP, corporate firewall and small businesses block all traffic on port 25.**

### Concepts

#### Mailboxes
Mailboxes are "rules" which dictates to which users incoming emails will be delivered to. Wildcards (*) are used to create catch-all mailboxes. Users can have multiple mailboxes on multiple domains. 
#### Identity
Identities are email addresses and display name combinations offered to users as expeditor when sending emails. Users can only send email as identities for which they are assigned.

## Disclaimer
In active development, partial features, missing features, security issues, not tested, etc.

## Screenshots
### Login with one time passwords
![Screenshot from 2024-03-20 06-36-55](https://github.com/monaha-hundo/websmtp/assets/139830086/07f97399-9856-4b7f-809a-3846e1424176)
![Screenshot from 2024-03-20 06-37-36](https://github.com/monaha-hundo/websmtp/assets/139830086/4bb75fb9-9352-45fb-9718-1da200e3e52d)

### Basic Inbox/All Mail/Favorites/Trash view
![Screenshot from 2024-03-15 08-43-49](https://github.com/monaha-hundo/websmtp/assets/139830086/d64d1654-5321-4ace-91e7-8688c37ce7b2)

### Detailed email view
![Screenshot from 2024-03-15 08-43-59](https://github.com/monaha-hundo/websmtp/assets/139830086/5cacbaf8-141d-4a14-8fb0-070a1dd843bd)

### Send Emails
![Screenshot from 2024-03-19 14-20-08](https://github.com/monaha-hundo/websmtp/assets/139830086/fee58ee6-8396-4cdc-a2b0-4f267455609a)

### Supports raw message display
![Screenshot from 2024-03-15 08-44-24](https://github.com/monaha-hundo/websmtp/assets/139830086/a3d650cf-b5a3-4fe6-b531-721935a78378)

### HTML Email with media support and attachements download
![Screenshot from 2024-03-19 14-17-21](https://github.com/monaha-hundo/websmtp/assets/139830086/accaea14-974a-4603-b1b9-d2043b79fd22)

### Quick OTP setup with QR code
![Screenshot from 2024-03-19 14-14-38](https://github.com/monaha-hundo/websmtp/assets/139830086/dc78ddbd-3628-4a52-9170-91dd9af9bbb5)
![Screenshot from 2024-03-19 14-14-48](https://github.com/monaha-hundo/websmtp/assets/139830086/2b5fd93b-e09f-4262-9789-70dd077b7f7e)

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