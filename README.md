# CrossApp
Наскрізний проєкт з крос-платформного програмування.
Предметна область: Замовлення. Сутності: Customer, Product, Order, OrderLine.
Призначення: оформлення замовлень і підрахунок сум.

## Структура

```text
CrossApp/
├── .gitignore
├── CrossApp.sln
├── README.md
├── data/
│   └── orders.json
├── src/
│   ├── Cli/
│   │   ├── Cli.csproj           
│   │   ├── Program.cs
│   │   └── StoreFactory.cs
│   └── Core/
│       ├── Core.csproj
│       ├── DomainException.cs                [НОВЕ: бізнес-винятки]
│       ├── EnvironmentInfo.cs
│       ├── SampleData.cs
│       ├── Abstractions/
│       │   ├── ICatalogImporter.cs           [НОВЕ: сумісний контракт імпорту]
│       │   ├── INotificationSink.cs
│       │   ├── IOrderImporter.cs             [НОВЕ: асинхронний контракт імпорту]
│       │   └── IOrderStore.cs
│       ├── Domain/
│       │   ├── Order.cs                      [ЗМІНЕНО: DomainException для бізнес-правил]
│       │   ├── OrderLine.cs
│       │   └── OrderStatus.cs
│       ├── Dto/
│       │   ├── CustomerDto.cs
│       │   ├── ImportResult.cs
│       │   ├── ImportRowDto.cs
│       │   ├── OrderDto.cs
│       │   ├── OrderLineDto.cs
│       │   └── ProductDto.cs
│       ├── Events/
│       │   ├── OrderChangedEventArgs.cs
│       │   └── OrderChangeKind.cs
│       ├── Import/
│       │   ├── CsvImporter.cs
│       │   ├── JsonImporter.cs
│       │   └── OrderImporter.cs
│       ├── Reports/
│       │   ├── CustomerOrdersSummary.cs
│       │   ├── GroupSummary.cs
│       │   ├── LinqExtensions.cs
│       │   ├── OrderReportLine.cs
│       │   └── ReportService.cs
│       ├── Services/
│       │   ├── CatalogService.cs             [НОВЕ: сумісна обгортка сервісу]
│       │   ├── ConsoleSink.cs
│       │   ├── FileSink.cs
│       │   └── OrderService.cs               [ЗМІНЕНО: ImportAsync, ImportStreamAsync]
│       └── Storage/
│           ├── CachingOrderStore.cs
│           ├── FileOrderStore.cs
│           ├── InMemoryOrderStore.cs
│           ├── JsonCatalogImporter.cs        [НОВЕ: async JSON імпортер]
│           └── JsonOrderImporter.cs          [НОВЕ: async читання та IAsyncEnumerable]
└── tests/
    └── Core.Tests/                           [НОВЕ: проєкт xUnit unit-тестів]
        ├── Core.Tests.csproj
        ├── CatalogServiceTests.cs
        ├── ImportTests.cs
        ├── ReportServiceTests.cs
        └── TestData.cs
```

## Запуск
`dotnet build` 

`dotnet run --project src/Cli`

- `--file` - запуск асинхронного імпорту замовлень (або використання файлового сховища для звітів)
- `--cache` - обгортка кешованого сховища над IOrderStore
- `--stream` - асинхронний потоковий імпорт через `IAsyncEnumerable<Order>`
- `--timeout [сек]` - демонстрація скасування за таймаутом (за замовчуванням 1 мс або задана кількість секунд)

## Публікація
- framework-dependent версія:
`dotnet publish src/Cli -c Release -r win-x64 --self-contained false`

- self-contained версія:
`dotnet publish src/Cli -c Release -r win-x64 --self-contained true`

## Середовище
.NET SDK 10.0, Windows 11 x64

## Таблиця RID
| RID |                                     Режим | Розмір publish | Потрібен runtime |
|---|------------------------------------------:|---------------:|-----------------:|
| win-x64 |                            self-contained |   76,6762475967407 MB |               ні |
| win-x64 |                       framework-dependent |   0,190942764282227 MB |    так (.NET 10) |
| win-x64 |   self-contained з PublishSingleFile=true |   70,1514854431152 MB |               ні |
| win-x64 |      self-contained з PublishTrimmed=true |   19,1592626571655 MB |               ні |
| linux-x64 |                            self-contained |   73,939427 MB |               ні |

## Інваріанти доменної моделі

### Order

- Ідентифікатор замовлення (`Id`) не може бути порожнім. Перевіряється в `Order.Create`; порушення спричиняє `ArgumentException`.
- Ідентифікатор клієнта (`CustomerId`) не може бути порожнім. Перевіряється в `Order.Create`; порушення спричиняє `ArgumentException`.
- До підтвердженого замовлення не можна додавати рядки. Перевіряється в `Order.AddLine`; порушення бізнес-правила спричиняє `DomainException` («Замовлення вже підтверджено»).
- Порожнє замовлення не можна підтвердити. Перевіряється в `Order.Confirm`; порушення спричиняє `DomainException` («Замовлення не має рядків»).
- Замовлення не можна підтвердити повторно. Перевіряється в `Order.Confirm`; порушення спричиняє `DomainException`.
- Підтверджене замовлення не можна скасувати. Перевіряється в `Order.Cancel`; порушення спричиняє `DomainException`.

### OrderLine

- Ідентифікатор товару (`ProductId`) не може бути порожнім. Перевіряється в `OrderLine.Create`; порушення спричиняє `ArgumentException`.
- Назва товару (`Name`) не може бути порожньою. Перевіряється в `OrderLine.Create`; порушення спричиняє `ArgumentException`.
- Ціна (`Price`) не може бути від’ємною; нульова ціна дозволена. Перевіряється в `OrderLine.Create`; порушення спричиняє `ArgumentOutOfRangeException`.
- Кількість (`Quantity`) має бути більшою за нуль. Перевіряється в `OrderLine.Create`; порушення спричиняє `ArgumentOutOfRangeException`.

### Відновлення з DTO

`Order.FromDto` відновлює замовлення через `Create`, `AddLine` і, за потреби, `Confirm`. Дані проходять ті самі перевірки, що й під час створення замовлення. У разі некоректних даних імпортер обгортає виняток у `DomainException` із зазначенням номера запису (`$"Запис {i + 1}: {ex.Message}"`).

## Звіти LINQ

### Підкоманди CLI
- `dotnet run --project src/Cli -f net10.0 -- report-top` - топ-5 замовлень за сумою зі стабільним детермінованим порядком.
- `dotnet run --project src/Cli -f net10.0 -- report-low` - замовлення з невеликою сумою (< 1000 грн).
- `dotnet run --project src/Cli -f net10.0 -- report-groups` - групування замовлень за статусом (`Draft`, `Confirmed`, `Cancelled`) з підрахунком кількості та суми.
- `dotnet run --project src/Cli -f net10.0 -- report-sum` - агреговані показники (кількість, загальна сума, середній чек, максимум).
- `dotnet run --project src/Cli -f net10.0 -- demo-deferred` - демонстрація відкладеного виконання запитів.
- `dotnet run --project src/Cli -f net10.0 -- report-join` - GroupJoin двох колекцій (Клієнти та Замовлення).
- `dotnet run --project src/Cli -f net10.0 -- benchmark` - бенчмарк Stopwatch впливу проміжного `ToList()`.