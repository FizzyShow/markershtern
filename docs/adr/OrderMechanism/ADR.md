### Context

Микросервис, реализующий механизм выставления заявок. 

ссылка на требования:  
https://docs.google.com/document/d/1NvxJDdTIB7qBqGpAQsgQmtSa3DbxsR0sPqAFgcczsjY/edit#heading=h.h3k0b09pdfj1   
https://docs.google.com/document/d/1JFq_g9cSWn0372pxw0ZwDewUuu7ypM6o/edit#heading=h.ayxh9rn5x1ly

---

### Status 

Предложено

---

### Decision

Приблизительный принцип работы микросервиса:  
\- Создание заявки. Facade делает первичную валидацию, что заявку выставить возможно, после чего отправляет сообщение через gRPC в Микросервис. На основе данных формируется заявка и сохраняется.   

\- Отмена заявки. От Facade получаем сообщение с ID пользователя и заявки, делаем поиск в БД, удаляем из Active. Отмененная заявка не заносится в историю сделок.   

\- Закрытие заявки. У микросервиса имеется фоновый процесс, который находит подходящие заявки и отправляет данные в топик Kafka, на который подписан микросервис транзакций.  

\- Отправка истории сделок пользователя. Микросервис от Facade получает запрос на предоставление истории сделок. В сообщении содержится ID пользователя. Микросервис делает поиск документа по ID в БД, после чего достает список Inactive и отправляет его Facade.  

\- Отправка списка активных заявок пользователя. Микросервис от Facade получает запрос на предоставление списка активных заявок. В сообщении содержится ID пользователя. Микросервис делает поиск документа по ID в БД, после чего достает список Active и отправляет его Facade.  

\- Отправка количества активных заявок пользователя. Микросервис от Facade получает запрос на предоставление количества активных заявок. В сообщении содержится ID пользователя. Микросервис делает поиск документа по ID в БД, после чего подсчитывает количество элементов в Active и отправляет его в Facade. 

\- Отправка рыночной стоимости продуктов пользователя. Микросервису от Facade приходит сообщение с ID пользователя и ID продуктов. После чего на основе этих данных выполняется поиск и вычисляется рыночная стоимость товаров и отправляется в Facade.

\- Удаление недействительных заявок. Микросервис подписан на топик изменения, удаления товара пользователя, куда отправляет сообщения микросервис портфеля. Сообщение
изменения содержит в себе ID пользователя, продукта, количество продукта в портфеле. Если количество продукта в портфеле меньше, чем в заявке, то она закрывается. Сообщение удаления содержит в себе ID пользователя, продукта. Микросервис делает поиск по ID, после чего удаляет заявки.

Заявка закрывается только после совершения транзакции.

БД микросервиса содержит записи по типу:

```
{
    "user_id" : "d28e5a67-8da4-4b76-97ce-1a648705c6e8",
    "orders" : {
      "active" : [
         {
            "order_id" : "91d6bbdf-6388-4da9-9991-9c92e8b751f7",
            "type" : 1,
            "product_id" : "b5a7c6d8-3533-4d45-97ba-500f202b9077",
            "quantity" : 3232,
            "price" : 123.00,
            "timelife" : 2017-09-09,
            "close_complete" : true,
         },
        ],
      "inactive" : [
         {
            "users_id_to" : [
	       {
	          "user_id" : "d6eef007-549b-4f94-b5f5-5a5738070dbc",
		  "quantity" : 300,
		  "price" : 300
	       },
	       {
	          "user_id" : "xdsefdsm-zxcb-4uj4-ikm5-53r53vbb0dbc",
		  "quantity" : 33,
		  "price" : 33
	       }
	    ],
            "order_id" : "31d6bbdf-6312-4da9-9881-9cx2e8b121f7",
            "type" : 2,
            "product_id" : "43a7c6d2-3543-4d75-97ba-500f20asdr37",
            "quantity" : 333,
            "price" : 333.00,
	    // Дата закрытия заявки
            "date_completed" : 2022-02-09,
            "close_complete" : false,
         },
        ]  
    }
  }
```

---

### Фоновый процесс   

Микросервис заявок должен иметь фоновый процесс обработки заявок, который будет закрывать подходящие. 

Доработаю этот процесс.

### .proto

Так как Proto не поддерживает Decimal, определяем свой:  
https://docs.microsoft.com/ru-ru/dotnet/architecture/grpc-for-wcf-developers/protobuf-data-types  
https://visualrecode.com/blog/csharp-decimals-in-grpc/

```proto
message DecimalValue {
  int64 units = 1;
  sfixed32 nanos = 2;
}
```

```proto
// Типы заявок
enum OrderTypes {
   // Тип заявок на продажу.
   Sell_order = 1;
   // Тип заявок на покупку.
   Buy_order = 2;
}
```

```proto
// Участник сделки
message OrderMember {
   string user_id = 1;
   int32 quantity = 2;
   DecimalValue value = 3;
} 
```


```proto
// Активная заявка.
message ActiveOrder {

   // Id заявки.
   string order_id = 1;
   
   // Тип.
   OrderTypes type = 2;
   
   // Id продукта.
   string product_id = 3;
   
   // Количество продукта.
   int32 quantity = 4;
   
   // Цена.
   DecimalValue value = 5;
   
   // Время жизни заявки.
   google.protobuf.Timespan timelife = 6;
   
   // Если true, то заявку нельзя закрыть частично.
   bool close_complete = 7;
}
```


```proto
// Неактивная заявка
message InactiveOrder {

   // Пользователи, которые участвовали в сделке.
   repeated OrderMember members = 1;
   
   // Id заявки.
   string order_id = 2;
   
   // Тип заявки.
   OrderTypes type = 3;
   
   // Id продукта
   string product_id = 4;
   
   // Общее количество товара.
   int32 quantity = 5;
   
   // Общая сумма использованная в сделке.
   DecimalValue value = 6;
   
   // Дата завершения сделки.
   google.protobuf.Timespan timelife = 6;
   
   // true - заявку не закрыть частично.
   bool close_complete = 7;
}
```

```proto
enum Errors {
	// Пользователь не найден.
	USER_NOT_FOUND = 1;
	// Пользователь с таким ID существует.
	USER_ID_MATCHES_EXISTING = 2;
	// Пользователь не имеет товара с таким ID
	USER_NOT_HAVE_PRODUCT = 3;
	// Пользователь не имеет необходимое количество товара.
	USER_NOT_HAVE_QUANTITY_PRODUCT = 4;
	// Продукт находится в продаже.
	PRODUCT_ON_SALE = 5;
	// У пользователя недостаточно денег.
	USER_NOT_HAVE_MONEY = 6;
}
```  

### Для gRPC c Facade

```proto   
// Сообщение приходит от Facade. На основе полей формируется заявка.
message CreateOrderRequest {
  string user_id = 1;
  
  // Тип заявки
  OrderTypes type = 7;
  string product_id = 2;
  DecimalValue price = 3;
  int32 quantity = 4;   
  
  // Время жизни заявки. Тут указана дата, до которой заявка действительна. 
  google.protobuf.Timespan timelife = 5;
  
  // Флаг указывающий, что заявка должна быть закрыта полностью.
  // true - полностью, false - можно дробить.
  bool close_complete = 6;
}
```  

```proto
// Сообщение для Facade, которое передает информацию о закрытой заявке.
message OrderIsDone {
   string user_id = 1;
   string order_id = 2;
   string user_id_to = 3; 
   DecimalValue value = 4;
   int32 quantity = 5;
   // Дата завершения заявки
   google.protobuf.Timespan date_completed = 6;
   bool close_complete = 7;
   OrderTypes type = 8;
}
```    

```proto
// Сообщение от Facade. Запрос на получение списка совершенных сделок.
message GetCompletedOrdersRequest {
   string user_id = 1;
}
```

```proto
// Сообщение для Facade. Ответ на запрос получения списка совершенных сделок.
message GetCompletedOrdersResponse {
   repeated InactiveOrder orders = 1;
}
```   

```proto
// Сообщение от Facade. Запрос на получение списка активных заявок.
message GetActiveOrdersRequest {
   string user_id = 1;
}
```

```proto
// Сообщение для Facade. Ответ на запрос получения списка активных заявок.
message GetActiveOrdersRequest {
   repeated ActiveOrder orders = 1;
}
```

```proto
// Сообщение от Facade. Запрос на получение количества активных заявок.
message GetOrderNumberRequest {
   string user_id = 1;
}
```

```proto
// Сообщение для Facade. Ответ на запрос получения количества активных заявок.
message GetOrderNumberResponse {
   int32 number = 1;
}
```

```proto
// Сообщение от Facade. Запрос на отмену активной заявки.
message CancleOrderRequest {
   // user_id используется для более быстрого поиска.
   string user_id = 1;
   string order_id = 2;
}
```   

```proto
// Сообщение от Facade. Запрос на получение стоимости портфеля.
message GetBriefcaseCostRequest {
   string user_id = 1;
   repeated string product_id = 2;
}
```

```proto
// Для отправки в Facade в ответ на запрос стоимости.
// Для данного сообщения следует найти более подходящее имя.
message ProductPrice {
   string product_id = 1;
   // Рыночная стоимость продукта на основе заявок.
   DecimalValue price = 2;
}
```


```proto
// Сообщение для Facade. Несет в себе информацию о рыночной стоимости продуктов.
// Почему не отправляем сразу сумму? Потому что в будущем понадобится рыночная стоимость для каждого товара.
message GetBriefcaseCostResponse {
   repeated ProductPrice prices = 1;
}
```

### для Apache Kafka   

```proto
// Микросервис подписан на топик события изменения количества товара в портфеле.
message Briefcase_ProductQuantityDecreaseEvent  {
   string user_id = 1;
   string product_id = 2;
   // Микросервис сверяет количество в сообщении с количеством в заявках пользователя в user_id.   
   // Если количество в заявке больше, то заявка должна закрыться.
   int32 quantity = 3;
}
```

```proto
// Микросервис подписан на топик события удаления количества товара в портфеле.
message Briefcase_ProductRemovedEvent {
   string user_id = 1;
   // Микросервис среди заявок user_id ищет заявки с таким product_id и удаляет их.
   string product_id = 2;
} 
```