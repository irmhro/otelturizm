import re, sys, pathlib, subprocess
base = 'https://localhost:7223'
root = pathlib.Path(r'C:\laragon\www\otelturizmnew\otelturizmnew')
cookie = root / 'partner_cookies.txt'
login_html = root / 'partner_login.html'
subprocess.run(['curl.exe','-k','-c',str(cookie),'-b',str(cookie),'-o',str(login_html), base + '/partner-giris'], check=True)
html = login_html.read_text(encoding='utf-8', errors='ignore')
m = re.search(r'name="__RequestVerificationToken" type="hidden" value="([^"]+)"', html)
if not m:
    print('TOKEN_NOT_FOUND')
    sys.exit(1)
token = m.group(1)
post_args = ['curl.exe','-k','-i','-c',str(cookie),'-b',str(cookie),'-X','POST',base + '/partner-giris',
    '-H','Content-Type: application/x-www-form-urlencoded',
    '--data-urlencode', f'__RequestVerificationToken={token}',
    '--data-urlencode', 'partnerIdentity=216silvertuzla@gmail.com',
    '--data-urlencode', 'partnerPassword=1585',
    '--data-urlencode', 'rememberMe=true']
result = subprocess.run(post_args, capture_output=True, text=True, check=False)
print(result.stdout)
