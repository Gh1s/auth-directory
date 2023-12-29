# Identity Provider

Ce projet contient l'API REST permettant de rechercher dans l'annuaire LDAP.
Elle utilise le gRPC LDAP de la stack d'authentification.

## 📁 Structure du projet

### `/docker`

Les fichiers de configuration pour les services dans Docker.

| Dossier                             | Description                                                                                                    |
| ----------------------------------- | -------------------------------------------------------------------------------------------------------------- |
| `/docker/certs`                     | Les certificats utilisés pour les terminaison TLS.                                                             |
| `/docker/certs/tls-grpc-ldap.*`     | Les fichiers du certificat X509 pour la terminaison TLS du gRPC `ldap` sur l'environnement `local`.            |
| `/docker/api`                       | Les fichiers de configuration pour l'API.                                                                      |

Le fichier `docker-compose-build.yaml` permet générer les images Docker de l'API.<br />
Le fichier `docker-compose-dev.yaml` permet de lancer les services requis pour l'environnement de développement et le tests.<br />
Le fichier `docker-compose-local.yaml` permet de lancer tous les services et l'API sur l'environnement `Local`.<br />
Le fichier `docker-compose-staging.yaml` petmet de lancer tous les services et l'API sur l'environnement `Staging`.
Le fichier `build.sh` permet de générer les images Docker pour tous les services de l'API.

> ℹ️ L'environnement `Development` est l'environnement par défaut lors des développements.<br />
> ℹ️ L'environnement `local` ou `Local` est l'environnement utilisé pour faire fonctionner toute la stack d'authentification sur une poste de développeur.<br />
> ℹ️ L'environnement `staging` ou `Staging` est l'environnement utilisé pour faire fonctionner toute la stack d'authentification en recette.

### `/protos`

Les fichiers [protobuf](https://developers.google.com/protocol-buffers).

### `/src`

Le code source.

### `/test`

Les tests unitaires, d'intégration et systèmes.

## 🧰 Tooling

### .NET

Le code source du projet est écris en [.NET 5](https://dot.net).

Pour le compiler il vous faudra le [SDK .NET 5](https://dotnet.microsoft.com/download).

### IDE

Comme IDE vous pouvez utiliser [Visual Studio](https://visualstudio.microsoft.com/en/downloads) ou [Rider](https://www.jetbrains.com/rider).

## 🔑 Keymaterials

Les certificats TLS sont repris depuis l'IDP.

#### Installation

Pour importer un certificat dans le store « Autorités de certification racines de confiance », depuis un invite de commande `Powershell` en mode admin :

```powershell
Import-Certificate -FilePath $cert_file_name -CertStoreLocation cert:\CurrentUser\Root
```
