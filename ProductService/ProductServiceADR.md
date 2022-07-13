ProductServices ("�������� ������ ������ �������, ��������� ��� ��������")

adr by team#2

#������:
	����������

# ��������:
	 ����������� � ��������:

		protos:

		������ ������
		���������
		�� ������� � mongodb
		����� ��������� �� kafka (crud)

	�������� ������ �������, ��������� ��� ��������


# �������:

	(����������� �������� �� ������� ���������� ������� - ������������ �����, ������� ����� ��� ������ �������)
### ����������� � ��������:

		PROTO 

			service ProductService
			{
				/// ����� ��� ����������� ������ ����������� ��������, 
				/// �������������� ��� ����� ����� ���������� ������������� �������� -
				/// ��� ���������� ������ � �������� �����, ��� � ���� ������� �������� �������� ����� (� ����?)
				rpc RegisterNewProduct(NewProductRequest) returns NewProductResponce
				
				/// ����� ��� ������ ������ �� ������ �� id ������
				rpc GetProduct(ProductRequest) returns ProductResponce

				rpc GetAllProducts(StreamProductsRequest) returns stream ProductResponce

			}



			message NewProductRequest
			{
				string Name = 1;
			}

			message NewProductResponce
			{
				bool Accepted = 1;

				string Id = 2;

				string Name = 3;
			}

			message ProductRequest
			{
				string Id = 1;
			}

			message ProductResponce
			{
				bool isFound = 1;

				string Id = 2;

				string Name = 3;
			}


		������ ������ ���� ���������:
		{
			[BSON.ObjectID]
			string Id

			[unique]
			string Name

		}

		�� ������� � mongodb
			���� ������ ���������� � ��������

		������ �������������� � �� �������
			// ����� ��������� ��� ������ ������ �� null/��������������, � ������������ � ����������� �������� ������������ �����
			����� ����������� ������ ������ � ��

			����� ������ ����������� ������ �� �� �� id

			����� ������ ������ ���� ���������� �������
			

		����� ��������� �� kafka (CRUD)
			��������� proto �������������� ������ �� c#-objs � ��������� � ���������� � ���� 

### �������� ������ �������, ��������� ��� ��������
		�������� ����� �������� � ������� ������������ ������
		����������� ������ ��������� � ����� ���������� ����� �� ��������� ������ ���� ������� � bid and ask

			service OrderService
			{
				/// �����-���� ������ ������, ������� ������������ � ������ ������ ����� ������������
				....

				/// ����� ��� ������� ������ ��������� ��� ��������
				/// ����� ������� bid and ask ��� ������� ������, �� ������� ���������� ������ � ��������� �����
				grpc GetProductsWithBidAndAsk(StreamProductsWithBidAndAskRequest) returns stream ProductWithBindAndAskResponce
			}

			message StreamProductsWithBidAndAskRequest
			{
				??? ��� ��������� ��� ���������� ���� �����-���� �����
			}

			message ProductWithBidAndAskResponce
			{
				string Id = 1;

				string Name = 2;

				float Bid = 3;

				float Ask = 4;

			}
			
### ����� � ������ �� ��������� ������ ����� �����
	�������� �������� � ������� ������������ ������,
	��� ������ ��������� � ����� ���������� ����� �� �������� ��� ������

		service FacadeService
		{
			/// �����-���� ������ ������, ������� ������������ � ������ ������ ����� ������������
			....

			/// ����� ��� �������� ��� ������ ��������� ��� ��������
			/// ����� ������� bid and ask ��� ������� ������, �� ������� ���������� ������ � ��������� �����
			grpc SendProductsWithBidAndAsk(StreamProductsWithBidAndAskRequest) returns stream ProductWithBindAndAskResponce
		}