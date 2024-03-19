# websmtp

Simple combined mail transfer agent and mail user agent with a web UI in C#.
Only support basic SMTP functionnalities. Completely insecure.
Purpose is a simple local mail "server" and "reader" without the hassle of setuping a _real_ mail server.
Use it to receive local email alerts from devices such as routers, ip cameras, printers, etc.

_In active development, partial features, missing features, etc._

## Login with one time passwords
![Screenshot from 2024-03-15 08-40-47](https://github.com/monaha-hundo/websmtp/assets/139830086/31cc34b3-8fbf-4d7e-b8ef-69c0274529b7)

## Basic Inbox/All Mail/Favorites/Trash view
![Screenshot from 2024-03-15 08-43-49](https://github.com/monaha-hundo/websmtp/assets/139830086/d64d1654-5321-4ace-91e7-8688c37ce7b2)

## Detailed email view
![Screenshot from 2024-03-15 08-43-59](https://github.com/monaha-hundo/websmtp/assets/139830086/5cacbaf8-141d-4a14-8fb0-070a1dd843bd)

## Send Emails
![Screenshot from 2024-03-19 14-20-08](https://github.com/monaha-hundo/websmtp/assets/139830086/fee58ee6-8396-4cdc-a2b0-4f267455609a)

## Supports raw message display
![Screenshot from 2024-03-15 08-44-24](https://github.com/monaha-hundo/websmtp/assets/139830086/a3d650cf-b5a3-4fe6-b531-721935a78378)

## HTML Email with media support
![Screenshot from 2024-03-19 14-17-21](https://github.com/monaha-hundo/websmtp/assets/139830086/accaea14-974a-4603-b1b9-d2043b79fd22)

## Quick OTP setup with QR code
![Screenshot from 2024-03-19 14-14-38](https://github.com/monaha-hundo/websmtp/assets/139830086/dc78ddbd-3628-4a52-9170-91dd9af9bbb5)
![Screenshot from 2024-03-19 14-14-48](https://github.com/monaha-hundo/websmtp/assets/139830086/2b5fd93b-e09f-4262-9789-70dd077b7f7e)


## Made with
- Asp.Net / .Net 8 / EntityFramework Core
- Bootstrap (no jquery/javascript)
- SweetAlert2
- MailKit / MimeKit
- MariaDb / Pomelo's MySQL Provider for .net ~~Oracle MySql Provider for efc~~
- Otp.Net
- QRCoder
- SmtpServer
- ~~bogus~~
