# websmtp

Simple combined mail transfer agent and mail user agent with a web UI in C#.
Think of it as self-hosted replacement for email services like gmail, outlook and proton mail.
Purpose is to be simple to setup and use while being somewhat flexible.
E.g. being a mock SMTP server for testing, home emails for LAN devices such as printers and ip cameras or your own webmail provider business.

_In active development, partial features, missing features, security issues, etc._

## Login with one time passwords
![Screenshot from 2024-03-20 06-36-55](https://github.com/monaha-hundo/websmtp/assets/139830086/07f97399-9856-4b7f-809a-3846e1424176)
![Screenshot from 2024-03-20 06-37-36](https://github.com/monaha-hundo/websmtp/assets/139830086/4bb75fb9-9352-45fb-9718-1da200e3e52d)

## Basic Inbox/All Mail/Favorites/Trash view
![Screenshot from 2024-03-15 08-43-49](https://github.com/monaha-hundo/websmtp/assets/139830086/d64d1654-5321-4ace-91e7-8688c37ce7b2)

## Detailed email view
![Screenshot from 2024-03-15 08-43-59](https://github.com/monaha-hundo/websmtp/assets/139830086/5cacbaf8-141d-4a14-8fb0-070a1dd843bd)

## Send Emails
![Screenshot from 2024-03-19 14-20-08](https://github.com/monaha-hundo/websmtp/assets/139830086/fee58ee6-8396-4cdc-a2b0-4f267455609a)
- DKIM Signing

## Supports raw message display
![Screenshot from 2024-03-15 08-44-24](https://github.com/monaha-hundo/websmtp/assets/139830086/a3d650cf-b5a3-4fe6-b531-721935a78378)

## HTML Email with media support and attachements download
![Screenshot from 2024-03-19 14-17-21](https://github.com/monaha-hundo/websmtp/assets/139830086/accaea14-974a-4603-b1b9-d2043b79fd22)

## Quick OTP setup with QR code
![Screenshot from 2024-03-19 14-14-38](https://github.com/monaha-hundo/websmtp/assets/139830086/dc78ddbd-3628-4a52-9170-91dd9af9bbb5)
![Screenshot from 2024-03-19 14-14-48](https://github.com/monaha-hundo/websmtp/assets/139830086/2b5fd93b-e09f-4262-9789-70dd077b7f7e)

## Configurable
Configuration in JSON in appSettings.json (environment controlled).
- Allowing the display of HTML content from emails (will display raw html instead of rendering it)
- Allowing the display of media (images, videos, etc.) in HTML email view (media will appear as broken).
(Note: possibility of parsing HTML server side to prepare/secure HTML for display, possibility of email hosting content localy, thus allowing stricter content security policy).

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
