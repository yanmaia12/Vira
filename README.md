# Conversor de Ficheiros

Aplicação Windows em C#/.NET para conversão local de imagens, atalhos de conversão no menu de contexto do Explorador e recolha de assinatura visual num PDF através do telemóvel.

## Ambiente e tecnologias

- Windows 10/11, .NET 8 SDK e Windows Desktop Runtime.
- WPF para a aplicação gráfica.
- Windows Imaging Component (WIC), através dos encoders/decoders do WPF, para conversão de imagens.
- PDFsharp 6.2.4 (licença MIT) para incorporar a imagem da assinatura no PDF.
- QRCoder 1.8.0 para gerar o QR code.
- ASP.NET Core/Kestrel, incluído no runtime .NET, para servir a página de assinatura na rede local.
- Registo por utilizador (`HKCU`) para atalhos no Explorador; não requer privilégios de administrador.

## Executar

Na pasta do repositório:

```powershell
dotnet restore ConversorFicheiros.sln
dotnet run --project ConversorFicheiros/ConversorFicheiros.csproj
```

Para criar uma publicação autónoma para Windows x64:

```powershell
dotnet publish ConversorFicheiros/ConversorFicheiros.csproj -c Release -r win-x64 --self-contained true
```

Executar os testes automatizados:

```powershell
dotnet test ConversorFicheiros.sln
```

## Funcionalidades nesta versão

- Adicionar múltiplas imagens por seletor de ficheiros ou arrastar para a janela.
- Converter imagens suportadas pelo WIC para PNG, JPEG, BMP, TIFF ou PDF. Cada frame da imagem fica numa página separada do PDF. O resultado é guardado ao lado do original.
- No botão **Adicionar ao Explorador**, criar comandos de contexto por utilizador para as extensões de imagem suportadas. A ação não instala shell extensions nem exige administrador.
- Selecionar um PDF, ler o QR com um telemóvel na mesma rede Wi-Fi, desenhar a assinatura e gravar uma cópia `_assinado.pdf` com a imagem na primeira página.
- Evitar substituir ficheiros de saída existentes, acrescentando um sufixo numérico quando necessário.

## Assinatura e privacidade

A assinatura é uma imagem desenhada e incorporada visualmente no PDF. Não é uma assinatura eletrónica qualificada, não usa certificado e não valida identidade ou integridade criptográfica do documento. A sessão é servida na rede local por HTTP e usa um token aleatório no URL; os dispositivos têm de conseguir comunicar entre si e a firewall do Windows pode pedir autorização para a aplicação. A cópia assinada é criada ao lado do original.

## Limites atuais

A conversão está limitada a imagens descodificadas pelo WIC, incluindo criação de PDF a partir de imagens. Ainda não converte documentos Office, áudio/vídeo ou PDF para imagens/outros formatos. A assinatura é posicionada no canto inferior direito da primeira página; não há ainda pré-visualização, seleção de página ou posicionamento. Atalhos registados no Explorador apontam para o executável em execução, por isso devem ser registados novamente depois de mover a publicação.

## Arquitetura e próximas etapas

Veja [docs/architecture.md](docs/architecture.md) para decisões, componentes e plano faseado. As extensões de formatos devem ser adicionadas como provedores/serviços de conversão independentes; a interface não deve conhecer detalhes específicos de cada biblioteca.
