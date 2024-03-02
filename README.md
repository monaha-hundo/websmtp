# websmtp

Simple combined mail transfer agent and mail user agent with a web UI in C#.
Only support basic SMTP functionnalities. Completely insecure.
Purpose is a simple local mail "server" and "reader" without the hassle of setuping a _real_ mail server.
Use it to receive local email alerts from devices such as routers, ip cameras, printers, etc.

_In active development, partial features, missing features, etc._

## Login with one time passwords
![Screenshot from 2024-02-26 09-47-51](https://github.com/monaha-hundo/websmtp/assets/139830086/5d7c567c-6136-4d50-8c31-3526101be3e3)

## Basic Inbox/All Mail/Trash view
![Screenshot from 2024-02-26 09-48-18](https://github.com/monaha-hundo/websmtp/assets/139830086/b1dee015-1e6f-4eee-a8b9-916dbcdc5cf2)

## Clean email view
![Screenshot from 2024-02-26 09-48-26](https://github.com/monaha-hundo/websmtp/assets/139830086/aba757c9-139b-4b52-8570-7b1d0a53ca1f)

## Send Emails
![Screenshot from 2024-03-02 13-32-52](https://github.com/monaha-hundo/websmtp/assets/139830086/8eb52502-d22c-4462-ad64-beb870d06b4b)

## Supports raw message display
![Screenshot from 2024-02-26 09-48-57](https://github.com/monaha-hundo/websmtp/assets/139830086/a21f1db8-3b4d-404a-8ae7-2d7edfad5af5)

## Supports HTML with media content
![Screenshot from 2024-02-26 09-49-14](https://github.com/monaha-hundo/websmtp/assets/139830086/ffd58730-f8e1-453b-9d34-64df1cace315)

## Quick OTP setup with QR code
![image](https://github.com/monaha-hundo/websmtp/assets/139830086/07812766-a779-4c36-9975-0dd26a4a60cb)


# Made with
- Asp.Net / .Net 8 / EntityFramework Core
- Bootstrap (no jquery/javascript)
- SweetAlert2
- MailKit / MimeKit
- MariaDb / Oracle MySql Provider for efc
- Otp.Net
- QRCoder
- SmtpServer
