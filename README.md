# Conversor de Ficheiros

Aplicação Windows em C#/.NET para conversão local de documentos Office, PDF e imagens, atalhos no menu de contexto do Explorador e recolha de assinatura visual num PDF através do telemóvel.

## Ambiente e tecnologias

- Windows 10/11, .NET 8 SDK e Windows Desktop Runtime.
- WPF para a aplicação gráfica.
- Automação COM do Microsoft Office instalado para preservar o layout dos formatos Word, Excel e PowerPoint.
- Windows Imaging Component (WIC), através dos encoders/decoders do WPF, para conversão de imagens.
- PDFiumRaster 2.0.5 (licença MIT) para rasterizar as páginas selecionadas e apresentar a pré-visualização para posicionamento.
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

## Instalar para uso normal

Para criar um instalador de utilizador (sem privilégios de administrador e sem exigir que o utilizador final tenha .NET instalado), execute no Windows:

```powershell
powershell -ExecutionPolicy Bypass -File installer/Build-Installer.ps1
```

O ficheiro `Vira-Setup.exe` (na raiz do repositório) instala a aplicação em `%LOCALAPPDATA%\Programs\Vira`, cria atalhos no Menu Iniciar e no Ambiente de Trabalho e regista os comandos do menu de contexto para esse utilizador. Depois da instalação, abra **Vira** pelos atalhos; não precisa executar `dotnet restore` ou `dotnet run` novamente. É uma publicação autónoma para Windows x64. As conversões Office continuam a requerer Microsoft Office instalado.

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
- Converter Word (`DOC`, `DOCX`, `RTF`, `TXT`, `HTML`, `ODT`) para PDF e outros formatos Word suportados pelo Office.
- Converter PDF para DOCX ou TXT através da importação do PDF pelo Word. A paginação e o layout podem mudar nesta conversão.
- Converter folhas Excel (`XLS`, `XLSX`, `XLSM`, `CSV`, `ODS`) para PDF, XLSX, XLS ou CSV; e apresentações PowerPoint (`PPT`, `PPTX`, `PPTM`, `ODP`) para PDF, PPTX ou PPT.
- Converter imagens suportadas pelo WIC para PNG, JPEG, BMP, TIFF ou PDF. Cada frame da imagem fica numa página separada do PDF. O resultado é guardado ao lado do original.
- No botão **Registar no Explorador**, criar comandos por utilizador para ficheiros Office, PDF e imagem. Não exige administrador. No Windows 11, os comandos podem estar em **Mostrar mais opções**.
- Selecionar um PDF, ler o QR com um telemóvel na mesma rede Wi-Fi, desenhar a assinatura, escolher a página, posicioná-la e redimensioná-la na pré-visualização e gravar uma cópia `_assinado.pdf`.
- Evitar substituir ficheiros de saída existentes, acrescentando um sufixo numérico quando necessário.

## Assinatura e privacidade

A assinatura é uma imagem desenhada e incorporada visualmente no PDF. Antes de guardar, é possível escolher a página e arrastar/redimensionar a assinatura nessa página. Não é uma assinatura eletrónica qualificada, não usa certificado e não valida identidade ou integridade criptográfica do documento. A sessão é servida na rede local por HTTP e usa um token aleatório no URL; os dispositivos têm de conseguir comunicar entre si e a firewall do Windows pode pedir autorização para a aplicação. A cópia assinada é criada ao lado do original.

## Limites atuais

A conversão de documentos requer Word, Excel ou PowerPoint instalados e depende das versões e filtros disponíveis no Office. Não cobre literalmente todos os formatos existentes, nem converte PDF para Excel/PowerPoint ou formatos multimédia. A assinatura pode ser aplicada a uma única página por operação; seleção de várias páginas, PDFs protegidos ou páginas rodadas ainda não são tratados especialmente. Atalhos registados no Explorador apontam para o executável em execução, por isso devem ser registados novamente depois de mover a publicação.

## Arquitetura e próximas etapas

Veja [docs/architecture.md](docs/architecture.md) para decisões, componentes e plano faseado. As extensões de formatos devem ser adicionadas como provedores/serviços de conversão independentes; a interface não deve conhecer detalhes específicos de cada biblioteca.
