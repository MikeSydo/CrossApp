# CrossApp

Навчальний .NET-проєкт із предметною областю замовлень:
`Customer`, `Product`, `Order`, `OrderLine`.

## Структура

- `src/Core` — доменна модель, DTO, імпортери, сервіси та сховища;
- `src/Cli` — консольний запуск і конфігурація;
- `src/Api` — Minimal API HTTP-сервіс (CRUD, валідація через FluentValidation, Scalar/OpenAPI);
- `tests/Core.Tests` — unit-тести доменного шару;
- `tests/Api.Tests` — інтеграційні тести API на `WebApplicationFactory`;
- `data` — локальні дані, які не додаються до git.

## Запуск HTTP API (Minimal API)

### Запуск сервера
Сервер Minimal API налаштований на порт `5080`:

```powershell
dotnet run --project src\Api --urls http://localhost:5080
```

Або використовуючи профіль запуску:
```powershell
dotnet run --project src\Api
```

### Документація та OpenAPI (Development)
- **OpenAPI v1 JSON**: [http://localhost:5080/openapi/v1.json](http://localhost:5080/openapi/v1.json)
- **Scalar UI**: [http://localhost:5080/scalar/v1](http://localhost:5080/scalar/v1)

### Приклади запитів через curl

1. Отримання списку замовлень із пагінацією та пошуком (GET):
```bash
curl -i "http://localhost:5080/api/orders?page=1&pageSize=5&search=Клавіатура"
```
*Повертає `{ "items": [...], "total": 18 }`.*

2. Створення нового замовлення (POST):
```bash
curl -i -X POST http://localhost:5080/api/orders \
  -H "Content-Type: application/json" \
  -d '{"customer":"C-001","lines":[{"productId":"P-001","name":"Клавіатура","price":850,"quantity":2}]}'
```
*Повертає `HTTP/1.1 201 Created` із заголовком `Location: /api/orders/{id}`.*

3. Отримання замовлення за ідентифікатором (GET):
```bash
curl -i http://localhost:5080/api/orders/O-001
```

4. Отримання загальної вартості замовлення (GET /{id}/total):
```bash
curl -i http://localhost:5080/api/orders/O-001/total
```

5. Повне оновлення замовлення (PUT):
```bash
curl -i -X PUT http://localhost:5080/api/orders/O-001 \
  -H "Content-Type: application/json" \
  -d '{"customer":"C-001","lines":[{"productId":"P-004","name":"USB-кабель","price":200,"quantity":3}]}'
```

6. Видалення замовлення (DELETE):
```bash
curl -i -X DELETE http://localhost:5080/api/orders/O-001
```
*Повертає `HTTP/1.1 204 No Content`.*

7. Перевірка видаленого ресурсу (GET після DELETE):
```bash
curl -i http://localhost:5080/api/orders/O-001
```
*Повертає `HTTP/1.1 404 Not Found`.*

8. Помилка валідації вхідних даних (POST з помилками валідації FluentValidation):
```bash
curl -i -X POST http://localhost:5080/api/orders \
  -H "Content-Type: application/json" \
  -d '{"customer":"","lines":[]}'
```
*Повертає `HTTP/1.1 400 Bad Request` у форматі ProblemDetails (RFC 9457) зі словником помилок у полі `errors`.*

9. Доменний конфлікт (POST з дубльованим товаром у рядках):
```bash
curl -i -X POST http://localhost:5080/api/orders \
  -H "Content-Type: application/json" \
  -d '{"customer":"C-001","lines":[{"productId":"P-001","name":"A","price":100,"quantity":1},{"productId":"P-001","name":"B","price":100,"quantity":1}]}'
```
*Повертає `HTTP/1.1 409 Conflict` через `DomainExceptionHandler`.*

## Порядок завантаження конфігурацій:

1. `src/Cli/appsettings.json`;
2. `appsettings.{Environment}.json`;
3. user-secrets;
4. змінні середовища з префіксом `CROSSAPP_`;
5. аргументи командного рядка.

Основні параметри:

```json
{
  "EnvironmentName": "Development",
  "Storage": {
    "DataPath": "data",
    "FileName": "orders.json",
    "Format": "json"
  },
  "Orders": {
    "Currency": "UAH"
  }
}
```

`FileOrderStore` читає та записує `OrderDto` у JSON. Перед заміною
`orders.json` він записує дані у `orders.json.tmp`, а потім виконує
атомарний `File.Move(..., overwrite: true)`. Сховище реалізує `IDisposable`
і видаляє тимчасовий файл, якщо він залишився після помилки.

Доступні формати `json` і `csv`. У CSV кожен рядок відповідає одному рядку
замовлення та містить `orderId`, `customerId`, `status`, `productId`, `name`,
`price` і `quantity`.

## Запуск

Із кореня репозиторію:

```powershell
dotnet run --project src\Cli --framework net10.0
```

Або для .NET 8:

```powershell
dotnet run --project src\Cli --framework net8.0
```

Власний шлях до даних:

```powershell
dotnet run --project src\Cli --framework net10.0 -- `
  --data-path C:\Temp\crossapp-data
```

`--data-path` і `CROSSAPP_Storage__DataPath` вказують на папку. У ній
автоматично створюється `data\orders.json` (або ім'я з `Storage:FileName`).
Якщо параметри не задані, використовується debug-сховище
`data\orders.json`. `--data-path` має пріоритет над змінною середовища.

Вибір CSV через аргумент командного рядка:

```powershell
dotnet run --project src\Cli --framework net10.0 -- --Storage:Format=csv --Storage:FileName=orders.csv
```

Вибір CSV через змінну середовища:

```powershell
$env:CROSSAPP_Storage__Format = "csv"
dotnet run --project src\Cli --framework net10.0
Remove-Item Env:CROSSAPP_Storage__Format
```

Імпорт і експорт підтримують обидва формати. Формат вхідного або вихідного
файлу визначається його розширенням:

```powershell
# Повністю замінити поточне сховище даними з CSV
dotnet run --project src\Cli --framework net10.0 -- `
  --import C:\Temp\orders.csv

# Повністю перезаписати інший файл поточним сховищем
dotnet run --project src\Cli --framework net10.0 -- `
  --export C:\Temp\orders.json
```

Під час імпорту джерело спочатку повністю читається та перевіряється.
Після успішної перевірки його вміст атомарно замінює активне сховище.
Під час експорту цільовий файл також повністю перезаписується атомарно.

Для імпорту CSV у JSON можна поєднати операцію з конфігурацією основного
сховища:

```powershell
dotnet run --project src\Cli --framework net10.0 -- `
  --Storage:Format=json `
  --Storage:FileName=orders.json `
  --import C:\Temp\orders.csv
```

Еквівалентний PowerShell override:

```powershell
$env:CROSSAPP_Storage__DataPath = "C:\Temp\crossapp-data"
dotnet run --project src\Cli --framework net10.0
Remove-Item Env:CROSSAPP_Storage__DataPath
```

Валюта замовлень задається ключем `Orders:Currency`:

```powershell
$env:CROSSAPP_Orders__Currency = "UAH"
dotnet run --project src\Cli --framework net10.0
Remove-Item Env:CROSSAPP_Orders__Currency
```

Або через command-line:

```powershell
dotnet run --project src\Cli --framework net10.0 -- --Orders:Currency=USD
```

Для bash:

```bash
CROSSAPP_Storage__DataPath=/tmp/crossapp-data \
  dotnet run --project src/Cli --framework net10.0
```

Legacy-форма для шляху також підтримується:

```powershell
$env:CROSSAPP_DATA_PATH = "C:\Temp\crossapp-data"
dotnet run --project src\Cli --framework net10.0
Remove-Item Env:CROSSAPP_DATA_PATH
```

## Secret

Secret не зберігається в `appsettings.json` або `.env`. Він читається з
user-secrets або environment variable:

```powershell
dotnet user-secrets set "Integration:ApiKey" "<локальне-значення>" --project src\Cli
$env:CROSSAPP_Integration__ApiKey = "<локальне-значення>"
```

Застосунок виводить лише факт наявності secret і його довжину, але не саме
значення.

## Перевірка

```powershell
dotnet build CrossApp.sln
dotnet test
```

## Публікація

Framework-dependent:

```powershell
dotnet publish src\Cli -c Release -r win-x64 --self-contained false
```

Self-contained:

```powershell
dotnet publish src\Cli -c Release -r win-x64 --self-contained true
```
