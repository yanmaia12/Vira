# Arquitetura e plano

## Contexto do ambiente

O ambiente de desenvolvimento tem Windows x64, SDK .NET 8.0.403, runtimes .NET 6/8 e Windows Desktop 6/8. Não havia ficheiros de projeto prévios nem ferramentas de conversão externas instaladas. Os pacotes NuGet são restauráveis, com acesso à rede autorizado durante a configuração.

## Decisões

1. **WPF em `net8.0-windows`:** suportado pelo runtime instalado e adequado à integração com o Explorador, drag and drop e uma interface nativa.
2. **Serviços isolados por responsabilidade:** `ImageConversionService`, `ExplorerMenuService`, `MobileSignatureServer` e `PdfSignatureService` separam a UI das integrações e facilitam a substituição de codecs.
3. **WIC para formatos de imagem:** usa codecs do Windows sem depender de executáveis instalados ou de uma biblioteca de imagem adicional. PDFsharp cria PDFs multipágina a partir de frames de imagem.
4. **PDFsharp para edição visual:** dependência MIT adequada à edição do PDF neste protótipo. A operação é explicitamente visual, não criptográfica.
5. **Página de assinatura local via Kestrel:** evita carregar o documento para um serviço externo. O QR contém endereço local e token aleatório; o upload é limitado a 5 MB e só aceita `image/png`.
6. **Contexto estático por utilizador:** comandos `HKCU\\Software\\Classes\\SystemFileAssociations` sem elevação. A implementação pode evoluir para um instalador/MSIX e integração moderna do menu Windows 11.

## Etapas

- [x] Inventariar SDK, runtime desktop, estado do repositório e codecs disponíveis.
- [x] Criar solução WPF, UI inicial e documentação de arquitetura.
- [x] Converter formatos de imagem comuns e integrar ações de menu de contexto por utilizador.
- [x] Criar fluxo QR, página de assinatura móvel e incorporação visual num PDF.
- [x] Acrescentar testes automatizados da conversão WIC, validação de formatos e ciclo HTTP.
- [ ] Adicionar pré-visualização e escolha de página/posição da assinatura, além de tratamento de PDFs protegidos.
- [ ] Avaliar provedores para Office/PDF e formatos multimédia, mantendo licenças e distribuição offline explícitas.
- [ ] Preparar instalador e desinstalação limpa dos atalhos do Explorador.

## Limitações e riscos técnicos conhecidos

- O fluxo QR depende de a rede permitir tráfego entre dispositivos e de uma regra de firewall para o processo. O servidor fecha ao terminar a assinatura ou a aplicação.
- A assinatura só é visual. PDFsharp pode não editar todos os PDFs protegidos, cifrados ou com estruturas incomuns.
- O serviço só aceita o MIME `image/png` no endpoint; os dados ainda dependem do descodificador do PDFsharp para validação completa.
- O menu de contexto atual cria chaves por extensão e formato. Deve haver uma operação correspondente de remoção na próxima etapa.
