# Fluently

<p align="center"><img src="mobile/assets/images/fluently_readme_logo.png" alt="Fluently" width="360"></p>
<p align="center">Aplicativo de aprendizagem de inglês com exercícios gerados em tempo real por IA.</p>

<p align="center">
<img src="https://img.shields.io/badge/.NET%2010-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 10">
<img src="https://img.shields.io/badge/Flutter-02569B?style=for-the-badge&logo=flutter&logoColor=white" alt="Flutter">
<img src="https://img.shields.io/badge/PostgreSQL-4169E1?style=for-the-badge&logo=postgresql&logoColor=white" alt="PostgreSQL">
<img src="https://img.shields.io/badge/OpenAI-412991?style=for-the-badge&logo=openai&logoColor=white" alt="OpenAI">
<img src="https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white" alt="Docker">
</p>

## Informações UNIUBE

- Disciplina: Desenvolvimento para Dispositivos Móveis
- Tutor: Mateus de Sousa Valente
- Avaliação: 08/08/2026 a 20/09/2026
- Valor: 20 pontos

## Pré-requisitos

- .NET SDK 10.0.400 ou compatível.
- Flutter com Dart SDK 3.13.3 ou compatível.
- PostgreSQL 12 ou superior.
- Docker Desktop, opcional.
- Cloudflare Tunnel, opcional.
- Chave de API da OpenAI.

## Configuração

~~~bash
cp .env.example .env
~~~

Preencha o .env:

~~~dotenv
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=troque
Jwt__SigningKey=troque
OpenAI__ApiKey=troque
~~~

## Executando

~~~bash
dotnet restore Fluently.slnx
dotnet run --project api/src/Fluently.API/Fluently.API.csproj
~~~

URLs locais: http://localhost:5229 e https://localhost:7042. O Swagger fica em http://localhost:5229/swagger no ambiente de desenvolvimento.

Para Docker:

~~~bash
docker compose up --build
~~~

A API ficará em http://localhost:8080.

Para acesso externo temporário com Cloudflare Tunnel:

Terminal 1:

~~~bash
cloudflared tunnel --url http://localhost:5229
~~~

Terminal 2, usando a URL exibida pelo túnel:

~~~bash
flutter run --dart-define=API_BASE_URL=https://<url-do-tunel>.trycloudflare.com
~~~

Com Docker, use `cloudflared tunnel --url http://localhost:8080`.

Para executar o aplicativo:

~~~bash
cd mobile
flutter pub get
flutter run --dart-define=API_BASE_URL=http://localhost:5229
~~~

Em um celular físico, troque localhost pelo IP do computador. Com Docker, use http://localhost:8080.

## Documentação

- [Documentação da API](api/README.md)
- [Documentação do aplicativo mobile](mobile/README.md)

## Licença

Consulte o arquivo [LICENSE](LICENSE).
