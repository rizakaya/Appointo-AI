# MCP / Tool Gateway

Bu dosya sadece Hafta 6 konusunu anlatir:

- Tool gateway ne is yapar
- Permission matrix ne is yapar
- Loglama neden gerekir
- Bu konuyu hangi orneklerle test edebilirsin

## Bu Kisim Ne Is Yapiyor?

Bu katman, agent ile backend arasina kontrollu bir ara katman koyar.

Kisa akis:

```text
Agent -> ToolGateway -> PermissionMatrix -> AppointmentService
```

Anlami:

- Agent hangi tool'u cagiracagina karar verir
- `ToolGateway` cagrayi alir
- `PermissionMatrix` yetkiyi kontrol eder
- `AppointmentService` is kurallarini uygular
- Sonuc doner
- Log kaydi olusur

## Bu Dersi Neden Ogreniyoruz?

Cunku agent dogrudan her seyi yapmamali.

Bu katman sayesinde:

- Yanlis tool kullanimi kontrol edilir
- Yetkisiz islem engellenir
- Tool cagrilari izlenebilir
- MCP mantigi daha net anlasilir

## Hangi Ornekle Test Etmeliyiz?

Bu ders icin en iyi baslangic ornegi:

`create_appointment`

Neden:

- Tool cagrisi gorunur
- Basarili sonuc gorunur
- Gateway akisi kolay takip edilir

Calistirma:

```bash
dotnet run --project src/Appointo.Console/Appointo.Console.csproj
```

Sonra sirasiyla sunlari dene:

```text
/tools
/demo-tool create
logs
```

Beklenen:

- `/tools` -> hangi tool'lar var gorursun
- `/demo-tool create` -> ornek bir randevu olusur
- `/logs` -> gateway uzerinden gecen kayitlari gorursun

## Ornek Senaryolar

### 1. Basarili Tool Cagrisi

Komut:

```text
/demo-tool create
```

Ne gosterir:

- `create_appointment` tool'u cagrilir
- Islem basarili olur
- Log'da `Attempt` ve `Completed` gorunur

### 2. Musait Slot Sorgusu

Normal chat mesaji:

```text
Yarin saat 14:00 icin sac kesim randevusu almak istiyorum.
Ahmet Kaya 0555 111 22 33
```

Ne gosterir:

- Agent normal kullanici mesajindan tool cagrisi yapar
- `create_appointment` yine `ToolGateway` uzerinden gecmektedir
- Sonra `/logs` yazinca bu cagrinin loglarini gorursun

### 3. Yetki Reddi

Komut:

```text
/demo-tool denied-cancel
```

Ne gosterir:

- `Guest` kullanici `cancel_appointment` cagirir
- Permission matrix islemi reddeder
- Log'da `Denied` gorunur

Bu ornek MCP mantigini anlamak icin cok onemlidir.

## En Kisa Test Akisi

Dersi hizli anlamak icin sadece bunu yap:

```text
/tools
/demo-tool create
/demo-tool denied-cancel
/logs
```

Bu 4 adim sana sunlari ayni anda gosterir:

- tool listesi
- basarili tool cagrisi
- yetki reddi
- loglama

## Dosya Referanslari

Kodun baktigin ana yerleri:

- [Program.cs](C:/projects/Lessons/Appointo%20AI/AppointoAI/src/Appointo.Console/Program.cs)
- [ToolGateway.cs](C:/projects/Lessons/Appointo%20AI/AppointoAI/src/Appointo.Tools/ToolGateway.cs)
- [PermissionMatrix.cs](C:/projects/Lessons/Appointo%20AI/AppointoAI/src/Appointo.Tools/PermissionMatrix.cs)

