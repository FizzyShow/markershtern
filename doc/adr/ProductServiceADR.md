
adr by ted7007

Статус:  
 * Предложено

Проблема:  

Мироксервис с товарами.  Основная задача, модель товара.  
Небходимо рассмотреть взаимодействие микросервиса с кафкой - сообщения, негативные сценарии, ошибки между сервисами
Также стоит вопрос о модели товара, взаимодействии сервиса товара с mongoDB

Требования:


Решение:

Микросервис работает с уникальными товарами  
  >Уникальность товара заключается в названии, не может быть две разновидности товара с одинаковым именем  
Микросервис будет создавать уникальные товары, выводить их по id или списком.

Модель товара: 

	{
		[BSON.ObjectID]
		string Id
		[unique]
		string Name 
	}
Наш микросервис подписан на топик , пусть будет NewProductRequest, куда будут приходить события на создание нового товара,  
предположительно запросы будут идти из микроервиса портфеля  
Как я вижу процесс получения запросов и общение с кафкой используя proto:  
Микросервис товаров опрашивает топик на новые сообщения.  
Каждое полученное сообщение будет десериализовыаться из байтовых значений в c#-objs используя десериализатор на основе proto NewProductRequest

	message NewProductRequest
	{
		string Name = 1;
	}
После того как сообщение было получено и десериализовано, через сервис продуктов, у которого будет доступ к контексту БД, проверяется название нового продукта на уникальность.  
Если товар не повторяет другое название - он успешно добавляется в список уникальных товаров, сервис возвращает id нового товара в БД и затем формируется ответ.  
Если товар повторяет другое название - то добавление его в список отклоняется и также формируется ответ
	
	message NewProductResponce
	{
		bool IsAccepted = 1;

		string Id = 2;

		string Name = 3;
	}
Если товар уникален - IsAccepted имеет значение true, остальные поля также заполняются,  
иначе IsAccepted равно false значению, а остальные поля не заполняются (дефолт).  
Ответ десериализуется также используя сериализацию на основе proto файла в байтовые значения.  
Теперь микросервис отправляет бинарное сообщение в ответный топик, пусть будет NewProductResponce.   

По поводу получения продюсером и консюмером proto файлов(для сериализации/десериализации) - я нашел решение, где эти proto файлы подключаются к каждому микросервису как библиотека.

Применение функции, которая будет возвращать товар по id я сейчас не вижу. А вот список товаров может использовать например микросервис заявок, когда будет спрашивать у пользователя название товара для оформления заявки.  
Получение списка уникальных товаров:  
Эта функция будет происходить вызываться другим сервисом по grpc, соотвественно снизу прилагаю контракт в proto:  


	service ProductService {
	  rpc GetAllProducts(AllProductsRequest) returns(AllProductsResponce);
	}
	
	message AllProductsRequest
	{
		
	}
	
	message AllProductsResponce
	{
		repeated ProductResponce Products = 1;
	}
	
	message ProductResponce
	{
		string Id = 2;

		string Name = 3;
	}
	
Альтернативные вариант ответного сообщения:
Передача нужного кол-ва следующих сообщений, количество можно например регулировать в запросе.

	message ProductResponce
	{
		string Id = 2;

		string Name = 3;
	}
Такое решение позволяет контролировать размер сообщения, когда выбранное мною решение отправляет все товары разом.  
 > Не вижу применения альтернативному варианту, ведь обычно требуются именно полный список уникальных продуктов
---  
Статус:  
 * Предложено

Проблема:  
Вывод списка товаров, доступных для торговли  

Сноска из [требований](https://docs.google.com/document/d/1NvxJDdTIB7qBqGpAQsgQmtSa3DbxsR0sPqAFgcczsjY/edit#):  
Система отображает список всех товаров, которые сейчас находятся в торговле, включая название товара, лучшую цена для покупки (bid), лучшую цена продажи (ask), которые формируются из всех текущих заявок для выбранного товара. 

Решение:  
Проблема будет решаться с помощью микросервиса заявок так как именно там формируются bid and ask.  
Сервис будет содержать в себе контракт для взаимодействия по grpc напрямую(кажется это называется клиент-серверное взаимодействие).  
Таким образом у нас не будет засоряться kafka query сообщениями

	service OrderService
	{
		// какие-либо другие методы, которые определяются в задаче самого этого микросервиса
		....

		/* метод для запроса списка продуктов для торговли
		 метод находит bid and ask для каждого товара, на который существуют заявки и формирует ответ */
		grpc GetProductsWithBidAndAsk(StreamProductsWithBidAndAskRequest) returns AllProductWithBindAndAskResponce
	}

	message AllProductsWithBidAndAskRequest {}

	message AllProductsWithBidAndAskResponce
	{
		repeat ProductWithBidAndAskResponce = 1;
	}

	message ProductWithBidAndAskResponce
	{
		string Id = 1;

		string Name = 2;

		DecimalValue Bid = 3;

		DecimalValue Ask = 4;

	}
	
	message DecimalValue
	{
		// The whole units of the amount.
		int64 units = 1;
		
	        // Number of nano (10^-9) units of the amount.
  		// The value must be between -999,999,999 and +999,999,999 inclusive.
 	        // If `units` is positive, `nanos` must be positive or zero.
		// If `units` is zero, `nanos` can be positive, zero, or negative.
 		// If `units` is negative, `nanos` must be negative or zero.
  		// For example $-1.75 is represented as `units`=-1 and `nanos`=-750,000,000.
		int32 nanos = 2;
	}
для DecimalValue нужно создать расширение для простого конвертирования в decimal, подробнее [тут](https://visualrecode.com/blog/csharp-decimals-in-grpc/)
	

---
Статус:  
 * Предложено

Проблема:  
Получение списка товаров доступных для торговли фасадом

Решение:  
Проблема решается с помощью микросервиса фасада.
Метод получения списка будет использовать контракт микросервиса заявок GetProductsWithBidAndAsk, делая на него запрос AllProductsWithBidAndAskRequest 
и ожидая ответ AllProductsWithBidAndAskResponce.  
Метод будет также отлавливать ошибку RpcException, если такая будет - фасад должен оповестить клиента о ошибке и попросить обновить страницу(по требованиям).