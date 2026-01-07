# 📱 ElectronicsShop API

Backend частина для інтернет-магазину електроніки. Проєкт побудований на платформі .NET з використанням сучасних архітектурних підходів для забезпечення масштабованості, тестованості та чистоти коду.

## 🚀 Огляд проєкту

Цей API забезпечує повний функціонал для роботи магазину: автентифікацію користувачів, управління каталогом товарів, обробку кошика, оформлення замовлень та систему відгуків.

### 🏗 Архітектура (Clean Architecture)

Проєкт розділено на 4 основні шари (Project Layers):

1. **Domain (`src/Domain`)**:
* Центральна частина системи. Містить сутності (Entities), об'єкти-значення (Value Objects), енамами (Enums) та винятки.
* Не має залежностей від інших шарів чи зовнішніх бібліотек.
* *Приклади:* `Product`, `Order`, `ApplicationUser`, `ProductId` (Strongly Typed ID).


2. **Application (`src/Application`)**:
* Містить бізнес-логіку програми.
* Реалізує патерн **CQRS** (розділення команд та запитів).
* Тут знаходяться Commands (наприклад, `CreateOrderCommand`), Queries (`GetProductByIdQuery`), Валідатори та Інтерфейси інфраструктури.


3. **Infrastructure (`src/Infrastructure`)**:
* Реалізація інтерфейсів, визначених у шарі Application.
* Робота з базою даних (EF Core Context, Repositories).
* Сервіси автентифікації (`JwtTokenGenerator`) та роботи з файлами (`LocalFileStorageService`).


4. **Api (`src/Api`)**:
* Точка входу в додаток.
* Містить контролери (`Controllers`), налаштування DI контейнера (`Program.cs`) та фільтри.
* Відповідає за прийом HTTP запитів та повернення відповідей.



## 🛠 Технологічний стек

* **Мова:** C#
* **Фреймворк:** ASP.NET Core (.NET 9)
* **База даних (ORM):** Entity Framework Core.
* **Автентифікація:** JWT Bearer Authentication (Identity).
* **Валідація:** FluentValidation.
* **Медіатор:** MediatR (для реалізації CQRS та Pipeline Behaviour).
* **Тестування:** xUnit, Integration Tests (`WebApplicationFactory`).

## 🧩 Патерни проєктування (Design Patterns)

У проєкті використано ряд ключових патернів:

* **CQRS (Command Query Responsibility Segregation):**
* Логіка читання (`Queries`) відокремлена від логіки запису/зміни (`Commands`). Це дозволяє оптимізувати запити та спростити підтримку коду.


* **Repository Pattern:**
* Використовується для абстракції доступу до даних (`IProductRepository`, `OrderRepository`).


* **Mediator Pattern:**
* Забезпечує слабку зв'язність між компонентами. Контролери не викликають сервіси напряму, а відправляють повідомлення (команду або запит) через медіатор.


* **Pipeline Behavior:**
* Використовується для наскрізної логіки, наприклад, автоматична валідація запитів перед їх обробкою (`ValidationBehaviour`).


* **Value Object / Strongly Typed IDs:**
* Замість звичайних `Guid` або `int`, використовуються спеціалізовані типи (`ProductId`, `OrderId`), що запобігає помилкам плутанини ID різних сутностей.


* **Result Pattern / Error Factory:**
* Централізована обробка помилок через фабрики помилок (`UserErrorFactory`, `ProductErrorFactory`).



## 📦 Основний функціонал

### 👤 Користувачі (Users & Auth)

* Реєстрація та логін (JWT).
* Ролі (Admin, User).
* Оновлення профілю, блокування/розблокування користувачів.

### 🛒 Товари (Products)

* CRUD операції для товарів.
* Завантаження зображень товарів (`LocalFileStorage`).
* Управління категоріями.
* Пошук та фільтрація.

### 🛍 Кошик (Cart)

* Додавання/видалення товарів.
* Оновлення кількості.
* Очищення кошика.

### 📦 Замовлення (Orders)

* Створення замовлення на основі кошика.
* Перегляд історії замовлень.
* Зміна статусу замовлення (для менеджерів).
* Скасування замовлення.

### ⭐ Відгуки (Reviews)

* Додавання відгуків до товарів.
* Модерація відгуків адміністратором.

## 📂 Структура папок

```text
src/
├── Api/              # Web API (Controllers, Program.cs)
├── Application/      # CQRS (Commands, Queries), DTOs, Interfaces
├── Domain/           # Entities, ValueObjects, Enums
└── Infrastructure/   # EF Core, Migrations, Services implementation

tests/
├── Api.Tests.Integration/ # Інтеграційні тестування контролерів
├── Tests.Data/            # Дані для тестів (Object Mothers/Builders)
└── Tests.Common/          # Спільна логіка для тестів

```

## 🚀 Як запустити

1. **Вимоги:**
* .NET SDK (версії 8.0 або вище).
* SQL Server (або інша БД, налаштована в `appsettings.json`).


2. **Налаштування БД:**
Змініть рядок підключення `DefaultConnection` у файлі `src/Api/appsettings.json`.
3. **Застосування міграцій:**
```bash
cd src/Api
dotnet ef database update

```


4. **Запуск API:**
```bash
dotnet run

```


API буде доступне за адресою `https://localhost:7099` (або `http://localhost:5052`).

## ✅ Тестування

Для запуску інтеграційних тестів виконайте команду:

```bash
dotnet test

```

---

*Цей README створено автоматично на основі аналізу файлової структури проєкту.*
