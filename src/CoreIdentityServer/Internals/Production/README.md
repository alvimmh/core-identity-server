# Setup

For production environment setup, follow the instructions below:

1. Initialize new hosting environment with encrypted storage;

2. Assign permanent ip (Elastic Ip for AWS) to the instance;

3. In your local secured machine, clone the repository and checkout branch `production`;

4. Then configure the application secrets for production environment;

5. Publish the app using command `dotnet publish --configuration Release`;

6. Copy over the `src/CoreIdentityServer/bin/Release/net6.0/publish` directory and `src/CoreIdentityServer/keys` directories to the hosted instance inside the `~/cis_application` directory using `scp`;

7. Connect to the hosted instance;

8. If its a new instance, update packages using these commands `sudo apt list --upgradable, sudo apt update, sudo apt upgrade`;

9. Install dotnet sdk from https://docs.microsoft.com/en-us/dotnet/core/install/linux;

10. Install nginx using command `sudo apt install nginx`;

11. Install certbot using command `sudo apt install certbot` and create a new certificate for domains `bonicinitiatives.biz` & `*.bonicinitiatives.biz` using command `sudo certbot certonly --manual --preferred-challenges dns`;

12. Generate a local dev SSL certificate so HTTPS redirection is possible for the CIS application using command `dotnet dev-certs https --trust`;

13. Configure nginx by copying the contents of `src/CoreIdentityServer/Internals/Production/nginx_default_configuration.txt` inside the local machine and then overwriting the contents of the file `/etc/nginx/sites-available/default` inside the hosted instance using command `sudo nano /etc/nginx/sites-available/default`;

14. Verify changes using command `sudo nginx -t`;

15. Create a service file inside the hosted instance using command `sudo nano /etc/systemd/system/cis.service` and overwriting its contents with that of the file `src/CoreIdentityServer/Internals/Production/cis.service` from the local machine;

16. Verify changes using command `sudo service cis status`;

17. Install font `fonts-liberation` using command `sudo apt install fonts-liberation`;

17. Restart nginx using command `sudo service nginx restart` to load recent configuration changes;

18. Start the cis service using command `sudo service cis start`;

19. Exit the hosted instance if the site is available over the internet;
