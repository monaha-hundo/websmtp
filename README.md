[websmtp.webm](https://github.com/monaha-hundo/websmtp/assets/139830086/3f4b4234-9442-408c-90d4-ea73ed4ada39)

# Description
Standalone SMTP server and web mail application. Purpose is to be simple to setup and use while being somewhat flexible. 

Think of it as self-hosted replacement for email services like gmail, outlook and proton mail.
Spam handling is done through Apache's Spam Assassin.

Possible uses: mock SMTP server for testing, home emails for LAN devices such as printers and ip cameras, your own web email provider business.

# Quick Start
## With Docker
1. Create a mariadb database named `websmtp` and a login/user with the `websmtp/websmtp` access credentials.
2. `docker run -it -p 5000:5000 -p 25:25 -e Database__Server=your-database-server yvansolutions/websmtp:latest ./websmtp --migrate-database --enable-admin --username=admin --password=admin`.
3. Navigate to `http://localhost:5000` and use the default `admin/admin` credentials.
## With  Docker-Compose
1. `docker compose up`
2. Navigate to `http://localhost:5000` and use the default `admin/admin` credentials.

## Running
### Binary
By default the app is published as self-contained single file executable. 
Running the app once configured is done by running the `websmtp` binary.
You can use the `--migrate-database` command line argument to prepare a database.

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
- `--migrate-database-only`: same as previous, but exit the application once done.
- `--enable-admin`: enable/create an admin user, similar to `--add-user` but if the username is taken, skip.
  - `--username=`
  - `--password=`
  - `--domain=`:
- `--generate-dkim-config`: generate the pub/priv certs, DNS record data and the configuration file data to enable DKIM signing and DNS configuration.

### Docker
Available on the docker hub: [yvansolutions/websmtp](https://hub.docker.com/r/yvansolutions/websmtp).
#### Building
Use the provided `build_docker.sh` which will: 
- compile/publish the app in the `build` folder
- launch docker build
- add the `build` folder to the container
- setup spam assassin
- tag the resulting image as yvansolutions/websmtp.

#### Launching
`docker run -it -p 5000:5000 -p 25:25 -e Database__Server=your-database-server yvansolutions/websmtp:latest ./websmtp --migrate-database --enable-admin --username=admin --password=admin`

This would launch the app, which would use `some-database-server` with the default database named `websmtp` with default credentials of `websmtp/websmtp`. It would listen for HTTP connections on port `5000` and SMTP `25`. An admin account with a catch-all mailbox and an identity of `postmaster@localhost`.

#### Configuring
Here are some important environment variables and their default:

`Database__Server: localhost`
`Database__Name: websmtp`
`Database__Password: websmtp`
`Database__Username: websmtp`

`SMTP__Port: 25`
`SMTP__RemotePort: 25`

`SpamAssassin__Enabled: True`

`DKIM__SigningEnabled: False`

`DNS__IP: 1.1.1.1`
`DNS__Port: 53`

`Security__EnableHtmlDisplay: True`
`Security__EnableMediaInHtml: True`
`SPF__Enabled: True`

# Disclaimer
In active development, partial features, missing features, security issues, not tested, etc.

# Made with
- [ .Net 8 / Asp.Net / EntityFramework Core](https://dotnet.microsoft.com/)
- [Bootstrap](https://getbootstrap.com/)
- [MailKit](https://github.com/jstedfast/MailKit) & [MimeKit](https://github.com/jstedfast/MimeKit) 
- [MariaDb](https://mariadb.org/) & [Pomelo's MySQL Provider for .net](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql)
- [SweetAlert2](https://sweetalert2.github.io/)
- [Otp.Net](https://github.com/kspearrin/Otp.NET)
- [QRCoder](https://github.com/codebude/QRCoder)
- [SmtpServer](https://github.com/cosullivan/SmtpServer)
- [Apache's Spam Assassin](https://spamassassin.apache.org/)

# Screenshots
![1](https://github.com/monaha-hundo/websmtp/assets/139830086/cb880b72-db72-428b-a60c-7dfbbe7d8114)
![2](https://github.com/monaha-hundo/websmtp/assets/139830086/0e28c450-d3a7-4935-8d99-d8dd52594f89)
![Screenshot from 2024-03-28 07-53-21](https://github.com/monaha-hundo/websmtp/assets/139830086/317654d2-cd1b-40a4-9453-f01ab9669c2f)
![Screenshot from 2024-03-28 07-53-32](https://github.com/monaha-hundo/websmtp/assets/139830086/cdb07c28-9810-4917-8d58-aa85e89bc6f6)
![Screenshot from 2024-03-28 07-55-47](https://github.com/monaha-hundo/websmtp/assets/139830086/e715888c-fe1d-46f8-883c-32f93fc62732)
![Screenshot from 2024-03-28 07-55-57](https://github.com/monaha-hundo/websmtp/assets/139830086/991b504b-457c-485e-ba88-017aa3482490)
![Screenshot from 2024-03-28 07-56-02](https://github.com/monaha-hundo/websmtp/assets/139830086/31672560-b6cb-4a57-9394-89bbeb78c397)
