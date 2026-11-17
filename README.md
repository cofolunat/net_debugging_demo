# Fibonacci Service

Минимальный ASP.NET Core-сервис, возвращающий число Фибоначчи по индексу.

## Запуск

```bash
docker compose up --build
```

Проверка:

```bash
curl "http://localhost:8081/test?i=10"
```

Ожидаемый ответ:

```json
{"input":10,"fibonacci":55}
```

Допустимы индексы от `0` до `92`. Для остальных значений сервис возвращает
HTTP 400, поскольку `F(93)` уже не помещается в `Int64`.

По умолчанию сервис публикуется на порту `8081`. Его можно переопределить:

```bash
FIBONACCI_PORT=9090 docker compose up --build
```

Логи приложения выводятся в stdout и доступны командой:

```bash
docker compose logs -f fibonacci-service
```

## Удалённая отладка

Отладчик установлен в контейнере в `/vsdbg`. Compose включает `SYS_PTRACE` и
ослабляет seccomp-профиль, чтобы IDE могла подключиться к процессу `dotnet`.

Имя запущенного контейнера можно получить так:

```bash
docker compose ps
```

Для проверки отладчика:

```bash
docker compose exec fibonacci-service /vsdbg/vsdbg --help
```
