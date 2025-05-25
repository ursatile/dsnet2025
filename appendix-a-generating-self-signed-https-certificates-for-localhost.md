---
title: "Appendix A: Creating a Self-Signed SSL Certificate for localhost"
layout: module
nav_order: 101
summary: >
    How to use OpenSSL to create a self-signed certificate so you can run servers on localhost using https
typora-copy-images-to: assets\images
---

Here's how you can use `openssl` to create self-signed certificates for running HTTPS servers on localhost.

### Create a signing key

```bash
openssl genrsa -out localhost.key 1024
```

### Create a certificate signing request

```bash
openssl req -new -key localhost.key -out localhost.csr
```

When you're prompted for **Common Name**, specify `localhost`. (You can accept the default/blank values for everything else.)

### Sign the certificate:

```bash
openssl x509 -req -days 9999 -in localhost.csr -signkey localhost.key -out localhost.crt
```

### Install the certificate:

#### Windows

1. Double-click the `localhost.crt` file
2. Click **Install Certificate...**
3. For **Store Location**, accept the default of **Current User**
4. For **Certificate Store**, choose **Place all certificates in the following store**
5. **Browse...** and select **Trusted Root Certification Authorities**
6. Click **Next**
7. Click **Finish**

You should get a scary security warning - that means it worked. Click **Yes**:

![image-20211124204209707](assets/images/image-20211124204209707.png)

You should get this:

![image-20211124204317127](D:\Projects\github\ursatile\dsnet\assets\images\image-20211124204317127.png)

That means it's working. You can now use your `localhost.crt` and `localhost.key` to run servers on localhost over HTTPS without getting certificate validation warnings.

