# CertGuard Mini

**Protótipo de validação** para o sistema de **Controle de Certificados Digitais** do Alvras.

> **O que é?** Uma aplicação desktop que testa se é possível bloquear sites específicos usando um proxy HTTPS local, enquanto o certificado digital fica protegido apenas em memória (nunca no disco).

---

## Por que isso importa?

Quando um escritório de contabilidade usa certificados digitais dos clientes (para acessar e-CAC, SPED, Receita Federal, etc.), existe um risco:

- O certificado é instalado no Windows → **qualquer site/app pode usar**
- O usuário pode acessar sites **não autorizados** com o certificado
- Não existe controle nativo do Windows para isso

**CertGuard Mini** testa a solução: um **proxy local** que intercepta o tráfego e só permite acesso a sites pré-aprovados.

---

## Stack Tecnológica

| Tecnologia | O que é | Por que usamos |
|-----------|---------|----------------|
| **.NET 8.0** | Runtime da Microsoft (versão LTS) | Gratuito, performance nativa, acesso direto ao Windows |
| **WPF** | Framework de UI para Windows Desktop | Interface nativa, sem navegador, leve |
| **Unobtanium.Web.Proxy** | Biblioteca de proxy HTTPS para .NET | Intercepta tráfego HTTPS,filtra por domínio |
| **CommunityToolkit.Mvvm** | Padrão MVVM para WPF | Código organizado, fácil de manter |

---

## Pré-requisitos (o que instalar no Windows)

### 1. .NET 8.0 SDK

O SDK permite compilar e rodar o projeto.

**Como instalar:**

1. Acesse: https://dotnet.microsoft.com/download/dotnet/8.0
2. Clique em **"Download .NET SDK"** (versão 8.0.x)
3. Execute o instalador (`dotnet-sdk-8.0.x-win-x64.exe`)
4. Aceite os termos e complete a instalação
5. Reinicie o terminal/computador

**Como verificar se instalou:**

```bash
dotnet --version
# Deve mostrar: 8.0.xxx
```

### 2. Visual Studio 2022 (ou Visual Studio Code)

O IDE para editar e compilar o projeto.

**Visual Studio 2022 Community (gratuito):**

1. Acesse: https://visualstudio.microsoft.com/downloads/
2. Baixe o **Visual Studio Community 2022**
3. Na instalação, marque:
   - ✅ **.NET desktop development** (desenvolvimento desktop .NET)
   - ✅ **.NET 8.0 Runtime** (se disponível)
4. Complete a instalação

**Ou Visual Studio Code (mais leve):**

1. Acesse: https://code.visualstudio.com/
2. Baixe e instale
3. Instale a extensão **C# Dev Kit** (da Microsoft)

### 3. Git (para baixar o código)

1. Acesse: https://git-scm.com/download/win
2. Baixe e instale com as configurações padrão

---

## Como baixar o projeto

```bash
# Clone o repositório
git clone https://github.com/SEU-USER/CertGuardMini.git

# Entre na pasta
cd CertGuardMini
```

---

## Como compilar e rodar

### Opção 1: Terminal (mais rápido)

```bash
# Restaurar pacotes NuGet (baixa as dependências)
dotnet restore

# Compilar o projeto
dotnet build

# Rodar a aplicação (precisa ser Administrador)
dotnet run
```

### Opção 2: Visual Studio 2022

1. Abra o Visual Studio 2022
2. Clique em **"Abrir um projeto ou solução"**
3. Navegue até a pasta `CertGuardMini` e selecione `CertGuardMini.sln`
4. Aguarde o Visual Studio restaurar os pacotes (canto inferior direito)
5. Pressione **F5** ou clique em **"Iniciar"**

### Opção 3: Criar instalador (.exe)

```bash
# Publicar como .exe autônomo (sem precisar do .NET instalado)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# O .exe estará em:
# bin\Release\net8.0\win-x64\publish\CertGuardMini.exe
```

---

## Como testar

### Passo 1: Executar como Administrador

> **Importante:** O proxy precisa de permissões de administrador para configurar o tráfego do sistema.

- Clique com o botão direito em `CertGuardMini.exe`
- Selecione **"Executar como administrador"**

### Passo 2: Ativar o Proxy

1. A aplicação abre com um painel escuro
2. Clique no botão verde **"▶ ATIVAR PROXY"**
3. Aceite o aviso que aparece

### Passo 3: Testar bloqueio

1. Abra o navegador (Chrome, Edge ou Firefox)
2. Acesse: `https://download.dfe.sefin.ro.gov.br`
   - **Resultado esperado:** Página de bloqueio vermelha ❌
3. Acesse: `https://google.com`
   - **Resultado esperado:** Google abre normalmente ✅

### Passo 4: Testar regras

1. Na coluna da direita, veja as regras de domínio
2. Adicione um novo domínio na caixa de texto
3. Clique em "❌ Bloquear" ou "✅ Permitir"
4. Teste novamente no navegador

### Passo 5: Parar o Proxy

1. Clique no botão vermelho **"⏹ PARAR PROXY"**
2. O proxy é desativado e o tráfego volta ao normal

---

## Estrutura do Projeto

```
CertGuardMini/
│
├── CertGuardMini.sln          # Arquivo de solução (abre no Visual Studio)
├── CertGuardMini.csproj       # Configuração do projeto + dependências
├── README.md                  # Este arquivo
├── .gitignore                 # Arquivos ignorados pelo Git
│
├── App.xaml                   # Configuração da aplicação WPF
├── App.xaml.cs                # Code-behind do App
│
├── MainWindow.xaml            # Interface principal (layout)
├── MainWindow.xaml.cs         # Lógica da interface (botões, eventos)
│
├── Models/
│   └── CertificateInfo.cs     # Modelo de certificado + regras de domínio
│
└── Services/
    ├── CertBrokerService.cs   # Broker: gerencia certificado em memória
    └── ProxyService.cs        # Proxy: intercepta e filtra tráfego HTTPS
```

---

## Como funciona por dentro

```
┌─────────────────────────────────────────────────────────┐
│                    CERTGUARD MINI                        │
│                                                          │
│  ┌──────────────┐        ┌──────────────────────────┐   │
│  │  USUÁRIO     │        │  PROXY (porta 8888)      │   │
│  │  abre sites  │───────►│  intercepta HTTPS        │   │
│  └──────────────┘        └──────────┬───────────────┘   │
│                                     │                    │
│                          ┌──────────▼───────────────┐   │
│                          │  CERT BROKER             │   │
│                          │  verifica:               │   │
│                          │  - domínio permitido?    │   │
│                          │  - certificado ativo?    │   │
│                          │  - regra de bloqueio?    │   │
│                          └──────────┬───────────────┘   │
│                                     │                    │
│                          ┌──────────▼───────────────┐   │
│                          │  RESULTADO               │   │
│                          │  ✅ Permitido → acesso   │   │
│                          │  ❌ Bloqueado → 403      │   │
│                          └──────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
```

---

## Solução de Problemas

| Problema | Causa | Solução |
|----------|-------|---------|
| "Erro ao iniciar proxy" | Não está como Admin | Clique com botão direito → Executar como administrador |
| "Porta 8888 em uso" | Outro programa usando a porta | Feche o programa ou mude a porta no código |
| Navegador não usa proxy | Proxy não configurado no sistema | Verifique as configurações de proxy do Windows |
| "Pacote não encontrado" | NuGet não baixou | Execute `dotnet restore` no terminal |
| Build falha | .NET SDK não instalado | Instale o .NET 8.0 SDK (veja acima) |

---

## Próximos Passos

- [ ] Integrar com Laravel API (buscar regras de domínio do servidor)
- [ ] Heartbeat com servidor (verificação periódica)
- [ ] Validação de hash de aplicativos (que apps podem usar o cert)
- [ ] Instalação automática de CA raiz para HTTPS
- [ ] System Tray (minimizar para a bandeja do sistema)
- [ ] Build automático com GitHub Actions

---

## Licença

Projeto interno - Alvras © 2026
