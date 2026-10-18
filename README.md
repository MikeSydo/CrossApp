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
└── src/
    ├── Cli/
    │   ├── Cli.csproj           
    │   ├── Program.cs
    │   └── StoreFactory.cs
    └── Core/
        ├── Core.csproj
        ├── EnvironmentInfo.cs
        ├── SampleData.cs
        ├── Abstractions/
        │   ├── INotificationSink.cs
        │   └── IOrderStore.cs
        ├── Domain/
        │   ├── Order.cs
        │   ├── OrderLine.cs
        │   └── OrderStatus.cs
        ├── Dto/
        │   ├── CustomerDto.cs
        │   ├── ImportResult.cs
        │   ├── ImportRowDto.cs
        │   ├── OrderDto.cs
        │   ├── OrderLineDto.cs
        │   └── ProductDto.cs
        ├── Events/
        │   ├── OrderChangedEventArgs.cs
        │   └── OrderChangeKind.cs
        ├── Import/
        │   ├── CsvImporter.cs
        │   ├── JsonImporter.cs
        │   └── OrderImporter.cs
        ├── Reports/
        │   ├── CustomerOrdersSummary.cs
        │   ├── GroupSummary.cs
        │   ├── LinqExtensions.cs
        │   ├── OrderReportLine.cs
        │   └── ReportService.cs
        ├── Services/
        │   ├── ConsoleSink.cs
        │   ├── FileSink.cs
        │   └── OrderService.cs
        └── Storage/
            ├── CachingOrderStore.cs
            ├── FileOrderStore.cs
            └── InMemoryOrderStore.cs
```

## Запуск
`dotnet build` 

`dotnet run --project src/Cli`

- `--file` - запуск файлового сховища
- `--cache` - обгортка кешованого сховища над IOrderStore

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
- До підтвердженого замовлення не можна додавати рядки. Перевіряється в `Order.AddLine`; порушення спричиняє `InvalidOperationException`.
- Порожнє замовлення не можна підтвердити. Перевіряється в `Order.Confirm`; порушення спричиняє `InvalidOperationException`.
- Замовлення не можна підтвердити повторно. Перевіряється в `Order.Confirm`; порушення спричиняє `InvalidOperationException`.

### OrderLine

- Ідентифікатор товару (`ProductId`) не може бути порожнім. Перевіряється в `OrderLine.Create`; порушення спричиняє `ArgumentException`.
- Назва товару (`Name`) не може бути порожньою. Перевіряється в `OrderLine.Create`; порушення спричиняє `ArgumentException`.
- Ціна (`Price`) не може бути від’ємною; нульова ціна дозволена. Перевіряється в `OrderLine.Create`; порушення спричиняє `ArgumentOutOfRangeException`.
- Кількість (`Quantity`) має бути більшою за нуль. Перевіряється в `OrderLine.Create`; порушення спричиняє `ArgumentOutOfRangeException`.

### Відновлення з DTO

`Order.FromDto` відновлює замовлення через `Create`, `AddLine` і, за потреби, `Confirm`. Тому дані з DTO проходять ті самі перевірки, що й під час звичайного створення замовлення.

## Звіти LINQ

### Підкоманди CLI
- `dotnet run --project src/Cli -f net10.0 -- report-top` - топ-5 замовлень за сумою зі стабільним детермінованим порядком.
- `dotnet run --project src/Cli -f net10.0 -- report-low` - замовлення з невеликою сумою (< 1000 грн).
- `dotnet run --project src/Cli -f net10.0 -- report-groups` - групування замовлень за статусом (`Draft`, `Confirmed`, `Cancelled`) з підрахунком кількості та суми.
- `dotnet run --project src/Cli -f net10.0 -- report-sum` - агреговані показники (кількість, загальна сума, середній чек, максимум).
- `dotnet run --project src/Cli -f net10.0 -- demo-deferred` - демонстрація відкладеного виконання запитів.
- `dotnet run --project src/Cli -f net10.0 -- report-join` - GroupJoin двох колекцій (Клієнти та Замовлення)

### Відкладене виконання (Deferred Execution)

Оператор фільтрації `Where` у LINQ to Objects використовує відкладене виконання: у момент створення виразу (`var query = source.Where(...)`) обробка колекції ще не розпочинається, а лише конструюється запит. Реальне виконання запиту відбувається щоразу, коли викликається термінальний оператор (наприклад, `Count()`, `ToList()`) або виконується перебір через `foreach`.

Через це два виклики `query.Count()` у демонстраційному сценарії повертають різні значення (спочатку 6, а після додавання нового замовлення - 7): під час другого виклику `Count()` запит повторно обчислюється над уже модифікованою колекцією `source`. На відміну від цього, виклик `ToList()` одразу матеріалізує поточні результати у пам'яті: сформований об'єкт `snapshot` зафіксував стан на момент виклику, тому після наступного очищення джерела `source.Clear()` у ньому незмінно залишилися всі 7 записів.

### Порівняння Query Syntax та Method Syntax
Реалізовано звіт групування замовлень за статусом у двох еквівалентних формах:

**Method Syntax (`ByStatus`):**
```csharp
source
    .GroupBy(o => o.Status.ToString(), StringComparer.OrdinalIgnoreCase)
    .Select(g => new GroupSummary(g.Key, g.Count(), g.Sum(o => o.Total)))
    .OrderByDescending(s => s.TotalAmount)
    .ToList();
```

**Query Syntax (`ByStatusQuerySyntax`):**
```csharp
(from o in source
 group o by o.Status.ToString() into g
 orderby g.Sum(x => x.Total) descending
 select new GroupSummary(g.Key, g.Count(), g.Sum(x => x.Total)))
.ToList();
```

### Вимірювання часу (Stopwatch) на 100 000 записах: вплив `ToList()`
Виміряно час виконання запиту з вибіркою перших 100 елементів (`Take(100)`), що задовольняють умову фільтрації, на 100 000 згенерованих об'єктів `Order`:
- **Команда для запуску:** `dotnet run --project src/Cli -f net10.0 -- benchmark`

**Результати вимірювання:**
- **Без `ToList` у середині ланцюжка:** ~0–1 мс (~2 358 тіків).
- **З проміжним `ToList()` перед `Take(100)`:** ~17 мс (~176 957 тіків).
- **Прискорення:** варіант без проміжного `ToList` швидший у **~75 разів**.