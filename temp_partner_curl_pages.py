import pathlib, subprocess, re
base = 'https://localhost:7223'
root = pathlib.Path(r'C:\laragon\www\otelturizmnew\otelturizmnew')
cookie = root / 'partner_cookies.txt'
routes = ['/panel/partner/dashboard','/panel/partner/rezervasyonlar','/panel/partner/takvim-fiyatlar','/panel/partner/oda-yonetimi','/panel/partner/otel-bilgileri','/panel/partner/fotograflar','/panel/partner/performans','/panel/partner/degerlendirmeler','/panel/partner/finans','/panel/partner/tercihler','/panel/partner/724-destek']
for route in routes:
    r = subprocess.run(['curl.exe','-k','-L','-c',str(cookie),'-b',str(cookie), base + route], capture_output=True, text=True)
    text = r.stdout
    title = re.search(r'<h1>(.*?)</h1>', text, re.S)
    has_silver = '216 SILVER SUITE' in text
    print(f'PAGE|{route}|{r.returncode}|{title.group(1).strip() if title else ""}|{has_silver}')
