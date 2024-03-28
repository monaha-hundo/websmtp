# Setup websmtp with docker
This document assume a general host location for files at `/websmtp/`.

## Certificates
Certificates used by the apps are to be put in a location which is mounted at `/certificates` in the container.


### HTTPS
You must generate pub/priv keys to handle HTTPS traffic. Both file must be in "PEM" format so the app can load them as a X509Certificate.

Place both PEM files in the `/websmtp/certificates/` folder. By deault the app will try to load `ssl.key` and `ssl.crt`. You can configure those using the `SSL__PrivateKey` and `SSL__PublicKey` environement variable.

If not or misconfigured, the app won't launch past the early HTTPS initialisation.

### DKIM
Full DKIM configuration is beyond the scope of this document. You can use the `--generate-dkim-config` argument on the binary to get you started.
To enable DKIM signing in the app, put all the required private keys in the `/websmtp/certificates/`.
Use the following set of envirnement variable to configure each domain:
- `DKIM__Domains__X__Name` the domain name for which to enable DKIM signing.
- `DKIM__Domains__X__Selector`: the DKIM selector.
- `DKIM__Domains__X__PrivateKey` the private key file.


For each configured domains, replace X by 0...N, e.g.:
    `DKIM__Domains__0__Name`
    `DKIM__Domains__0__Selector`
    `DKIM__Domains__0__PrivateKey`
    `DKIM__Domains__1__Name`
    `DKIM__Domains__1__Selector`
    `DKIM__Domains__1__PrivateKey`
    etc.