# Rapid Light EVE Launcher

rlel (yes I hate this acronym too) is a replacement for the CCP launcher that supports launching both 
Tranquility and Singularity clients, as well as keeping those clients up to date automagically

<br>![alt](https://i.imgur.com/QtaQu9o.png)

## FAQ

### are passwords encrypted?

yes

### do you steal my password

no

### how does it work?

oh boy. EVE login is done through [OAuth2](http://community.eveonline.com/news/news-channels/eve-online-news/single-sign-on-comes-to-account-management/). normal SSO login happens at

    https://login.eveonline.com/Account/LogOn?client_id=evegate&scope=user

the CCP launcher loads

    https://client.eveonline.com/launcherv3/en?steam_token=&server=tranquility

which loads

    https://login.eveonline.com/Account/LogOn?ReturnUrl=/oauth/authorize/?client_id=eveLauncherTQ&lang=en&response_type=token&redirect_uri=https://login.eveonline.com/launcher?client_id=eveLauncherTQ&scope=eveClientToken

in a frame. the `client_id=eveLauncherTQ` field causes a completely different page to be presented. clicking "login" submits a POST to that same url with a standard form-encoded body

    UserName=raylu&Password=123

assuming your credentials are valid, at this point some cookies get set, a lot redirects happen (IIS addles the brain) and eventually you land at a URL like

    https://login.eveonline.com/launcher?client_id=eveLauncherTQ#access_token=...&token_type=Bearer&expires_in=43200

this gives you an access\_token that is valid for 43,200 seconds == 12 hours. at this point, rhel simply trades the access\_token for an SSO token by GETing

    https://login.eveonline.com/launcher/token?accesstoken=...

and looking at the redirect, which contains another `#access_token=...`. this SSO token is passed to the game

    bin\Exefile.exe /noconsole /ssoToken=... /triPlatform=dx11

the first access\_token is used to sign into various parts of the EVE infrastructure (forums, gate, etc.) and the second one, the SSO token, is used for the game

before login happened, the CCP launcher POSTed to

    https://client.eveonline.com/launcherv3/en/GetNewsList

with another form-encoded body of

    count=5&page=1&maxChars=160

(because IIS dulls the mind) and got a JSON response. it also sent a GET to

    https://client.eveonline.com/launcherv3/en/GetStatus

and got a JSON response. after logging in but before launching the game, it GETs

    https://client.eveonline.com/launcherv3/en/VerifyUser/

with that trailing slash (because IIS doesn't even care) and an `Authorization: Bearer ...` header containing the first access\_token. the response is some kind of fucked up backslash-escaped almost-JSON representation of some junk (because... OK, I can't continue blaming IIS)

### how do you guess the EVE installation path?

I look at the directories in `%LOCALAPPDATA%\CCP\EVE` and then cry myself to sleep
