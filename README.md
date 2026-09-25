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
└── src/
    ├── Cli/
    │   ├── Cli.csproj           
    │   └── Program.cs
    └── Core/
        ├── Core.csproj
        └── EnvironmentInfo.cs      
```

## Запуск
`dotnet build` 

`dotnet run --project src/Cli` 

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