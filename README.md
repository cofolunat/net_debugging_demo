# Debugging Demo

Учебный ASP.NET Core-сервис на .NET 8. Помимо расчёта чисел Фибоначчи,
проект содержит два примера побочных эффектов, возникающих при вычислении
выражений в отладчике:

- вызов getter изменяет состояние объекта;
- вызванный из Debug Console метод зависает в ожидании блокировки, которую
  удерживает остановленный поток.

## Маршруты

| Метод и маршрут | Назначение |
| --- | --- |
| `GET /fibonacci?i={index}` | Рассчитать число Фибоначчи |
| `GET /side-effect/getter` | Продемонстрировать побочный эффект в getter |
| `GET /side-effect/deadlock` | Удержать блокировку для демонстрации зависания expression evaluation |
| `GET /side-effect/deadlock/status` | Получить текущее значение счётчика блокировки |

## Локальный запуск

В VS Code выберите конфигурацию `LOCAL` и нажмите `F5`. Перед запуском задача
`build Debugging Demo` соберёт проект в конфигурации Debug. Сервис будет
доступен по адресу `http://localhost:8081`.

Запуск без VS Code:

```bash
ASPNETCORE_ENVIRONMENT=Development \
ASPNETCORE_URLS=http://localhost:8081 \
dotnet run
```

## Запуск в Docker

```bash
docker compose up --build
```

Compose собирает приложение в конфигурации Debug и публикует порт контейнера
`8080` на локальном порту `8081`. Локальный порт можно изменить:

```bash
DEBUGGING_DEMO_PORT=9090 docker compose up --build
```

## Расчёт числа Фибоначчи

```bash
curl "http://localhost:8081/fibonacci?i=10"
```

Ответ:

```json
{"i":10,"result":55}
```

Допустимы индексы от `0` до `92` включительно. Для отсутствующего параметра и
значений вне диапазона контроллер возвращает HTTP 400. `F(93)` уже не помещается
в `Int64`.

## Удалённая отладка контейнера

Docker-образ содержит `vsdbg` по пути `/vsdbg/vsdbg`. Compose добавляет
capability `SYS_PTRACE` и отключает seccomp-профиль, чтобы `vsdbg` мог
подключиться к уже работающему процессу `dotnet`.

1. Запустите контейнер в фоне:

   ```bash
   docker compose up --build -d
   ```

2. Узнайте его имя:

   ```bash
   docker compose ps
   ```

3. Конфигурация `REMOTE` в `.vscode/launch.json` выполняет:

   ```bash
   docker exec -i debugging_demo /vsdbg/vsdbg --interpreter=vscode
   ```

   Compose явно задаёт контейнеру имя `debugging_demo`, поэтому конфигурации
   отладки не требуется определять сгенерированное имя контейнера.

   Чтобы не зависеть от имени контейнера, можно использовать Compose-транспорт:

   ```json
   "pipeProgram": "docker",
   "pipeArgs": ["compose", "exec", "-T", "debugging_demo"]
   ```

4. Выберите конфигурацию `REMOTE`, нажмите `F5` и укажите процесс
   `dotnet DebuggingDemo.dll`.

`sourceFileMap` сопоставляет путь `/src`, использованный при сборке образа, с
локальным каталогом проекта, поэтому breakpoint устанавливаются в локальных
исходниках.

Настройки `SYS_PTRACE` и `seccomp:unconfined` ослабляют изоляцию контейнера и
предназначены только для разработки и отладки.

## Побочный эффект в getter

Класс `ExpEvalSEController` содержит свойство `I`, getter которого увеличивает
поле `i` при каждом чтении:

```csharp
public int I
{
    get
    {
        Interlocked.Increment(ref i);
        return Volatile.Read(ref i);
    }
}
```

Порядок проверки:

1. Запустите `LOCAL` или подключите `REMOTE`.
2. Поставьте breakpoint в `ExpEvalSEController.Get` на строке `return Ok(...)`.
3. Вызовите endpoint из отдельного терминала:

   ```bash
   curl "http://localhost:8081/side-effect/getter"
   ```

4. На breakpoint выполните несколько раз в Debug Console:

   ```csharp
   this.I
   ```

5. Продолжите выполнение. После трёх вычислений ответ будет выглядеть так:

   ```json
   {"iBeforeExpEval":0,"iAfterExpEval":3}
   ```

IDE может вычислить getter автоматически при наведении мыши, раскрытии объекта
или обновлении Watch. Поэтому значение `iAfterExpEval` иногда оказывается
больше числа явных вызовов — это и демонстрирует пример.

## Ожидание блокировки при expression evaluation

`DeadLockSEController.Get` получает монитор через `CounterWithLock.GetLock()` и
удерживает его до завершения запроса. Метод `CounterWithLock.Evaluate()` запускает
worker-задачу, которая пытается получить тот же монитор и увеличить счётчик.

Порядок проверки:

1. Запустите `LOCAL` или подключите `REMOTE`.
2. Поставьте breakpoint в `DeadLockSEController.Get` на строке `return Ok(...)`.
   В этой точке переменная `curLock` ещё удерживает монитор.
3. Вызовите endpoint из отдельного терминала:

   ```bash
   curl "http://localhost:8081/side-effect/deadlock"
   ```

4. На breakpoint один раз выполните в Debug Console:

   ```csharp
   counterWithLock.Evaluate(), run-all-threads
   ```

   `run-all-threads` — команда отладчику, а не часть синтаксиса C#. Она явно
   разрешает остальным потокам приложения выполняться во время function
   evaluation. Без неё отладчик может отказаться от вызова и вывести сообщение:

   ```text
   Cannot evaluate expression since the function evaluation requires all
   threads to run.
   ```

На пять секунд получится цикл ожидания:

```text
expression evaluation ожидает завершения worker-задачи
                         ↓
worker-задача ожидает монитор
                         ↓
монитор удерживает остановленный метод контроллера
```

В `CounterWithLock.Evaluate()` установлен защитный таймаут длительностью пять
секун. После него Debug Console вернёт строку:

```text
Таймаут: задача всё ещё ожидает блокировку
```

Worker-задача при этом останется ждать монитор. Продолжите выполнение по `F5`:
контроллер освободит монитор, worker-задача увеличит счётчик.

Проверить результат:

```bash
curl "http://localhost:8081/side-effect/deadlock/status"
```

После одного вычисления ожидается:

```json
{"counter":1}
```

Не добавляйте `counterWithLock.Evaluate()` в Watch: IDE может повторно вычислять
выражение на каждой остановке и создавать дополнительные worker-задачи и
побочные эффекты. Используйте вариант с `run-all-threads` только один раз и
только в Debug Console.
