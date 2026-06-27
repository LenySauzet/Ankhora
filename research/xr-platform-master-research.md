# Ankhora — Recherche maîtresse : développer une plateforme XR enterprise (Quest 3) en solo depuis Mac M4 Pro

> **Date** : 2026-05-29
> **Cible** : plateforme de masterclasses XR (hand tracking, spatial anchors, passthrough, QR launch, RAG chatbot), industries manufacturing / culinary arts
> **Stack dev** : Mac M4 Pro · Meta Quest 3 · Unity · Cursor · Claude Code · Warp · Blender · câble INIU USB-A→USB-C
> **Méthodologie de recherche** : fan-out 6 angles → 33 sources → 142 claims extraites → 25 fact-checkées en 3-vote adversarial → 19 confirmées, 6 refutées
> **Lecture** : 30–45 min. Ce document est conçu pour servir de contexte permanent à Claude Code / Cursor.

---

## TL;DR — 7 verdicts à retenir

1. **Mac M4 Pro est viable comme poste principal en 2026**, mais avec un workflow différent de Windows : pas de Quest Link (Windows-only, aucun plan macOS annoncé). Tu builds en APK et tu push via `adb` ou MQDH. Pour itérer vite sans flasher à chaque fois, tu utilises **Meta XR Simulator** (runtime OpenXR natif Apple Silicon).
2. **Pas besoin d'acheter un PC Windows** pour démarrer. Un Mac M4 Pro suffit pour 95 % du dev. Tu n'auras besoin de Windows que si tu veux du Quest Link "Editor Play Mode" sur device (gain confort, pas blocage).
3. **Le câble INIU** marche pour `adb` (transferts APK ~50–100 MB) et la charge passthrough. Il n'est pas adapté à Quest Link de toute façon — mais comme tu n'utiliseras pas Link, c'est sans impact. La dérivation chargeur est même un plus pour ne pas vider le casque pendant les sessions de dev.
4. **Stack SDK officiel** : Unity 6 LTS (ou rester sur 2022.3 LTS jusqu'à stabilité Meta) + **Meta XR All-in-One SDK** + **OpenXR plugin** + **XR Interaction Toolkit 3.x** + **XR Hands** + **Meta-OpenXR package** (Unity). Toujours via **Building Blocks** + **Project Setup Tool** pour l'auto-config.
5. **Hand tracking** : utiliser l'**Interaction SDK** (recommandation Meta explicite — implém custom = risque de rejet store). Pour les "ghost hands", `HandVisualizer` (XR Hands) ou `OVRHand`+`OVRSkeleton` (Core SDK).
6. **AI tooling Unity mature** : 2 MCP servers actifs (**CoplayDev/unity-mcp** 10k+ stars, **CoderGamester/mcp-unity** 1.7k stars) connectent Claude Code/Cursor à l'éditeur Unity — Cursor/Claude peut éditer scènes, scripts, lancer tests, builder.
7. **Spec-first** : **GitHub Spec-Kit** est l'option la plus mature en 2026 (officielle GitHub), **OpenSpec** (Fission-AI) en alternative légère, **cc-sdd** (gotalab) si tu veux du Kiro-style sur Claude Code. Pour Ankhora solo, structure `docs/` + `.claude/` + `CLAUDE.md` racine, sans framework lourd au début.

---

## Table des matières

1. [Pipeline Mac M4 Pro → Quest 3 (AXE 1)](#axe-1)
   - 1.1 [Réalité Mac vs Quest Link](#11-realite-mac-vs-quest-link)
   - 1.2 [Le trio Mac-natif : Simulator + MQDH + adb](#12-trio-mac-natif)
   - 1.3 [Câble INIU et contraintes USB](#13-cable-iniu)
   - 1.4 [Setup Unity step-by-step](#14-setup-unity-step-by-step)
   - 1.5 [Features XR critiques Ankhora](#15-features-xr-critiques)
   - 1.6 [Distribution Quest 3 (Store, App Lab, side-loading, MDM)](#16-distribution)
2. [Tooling IA pour XR/Unity (AXE 2)](#axe-2)
   - 2.1 [MCP servers Unity](#21-mcp-servers-unity)
   - 2.2 [MCP Blender](#22-mcp-blender)
   - 2.3 [Rules Cursor / skills Claude Code](#23-rules-cursor-skills-cc)
   - 2.4 [Subagents Claude Code recommandés](#24-subagents)
   - 2.5 [AI natifs Unity (Muse, Sentis)](#25-ai-natifs-unity)
   - 2.6 [Stack complémentaire studio](#26-stack-complementaire)
3. [Spec-first development (AXE 3)](#axe-3)
   - 3.1 [Comparatif Spec-Kit / OpenSpec / cc-sdd](#31-spec-frameworks)
   - 3.2 [Méthodologie AI-native (Grove, Karpathy)](#32-ai-native-methodo)
   - 3.3 [Structure docs/specs recommandée pour Ankhora](#33-structure-docs)
   - 3.4 [Découpe MVP vs V2 argumentée](#34-mvp-v2)
   - 3.5 [Ressources d'apprentissage](#35-ressources)
4. [Plan d'action 2 semaines](#plan-2-semaines)
5. [Annexes : sources, claims refutées, questions ouvertes](#annexes)

---

<a id="axe-1"></a>
## 1. Pipeline Mac M4 Pro → Quest 3 (AXE 1)

<a id="11-realite-mac-vs-quest-link"></a>
### 1.1 Réalité Mac vs Quest Link en 2026 ✅ vérifié

**Faits durs** :

| Capacité | Windows | macOS (Apple Silicon) |
|---|---|---|
| **Meta Quest Link** (Editor Play Mode sur device) | ✅ Officiel | ❌ Non supporté, **aucun plan annoncé** ([meta.com](https://www.meta.com/help/quest/140991407990979/)) |
| **Meta XR Simulator** (runtime OpenXR léger) | ✅ | ✅ Officiel sur Apple Silicon ([Meta blog juil. 2024](https://developers.meta.com/horizon/blog/mac-support-unity-meta-quest-horizon-developer/)) |
| **Meta Quest Developer Hub (MQDH)** | ✅ | ✅ Natif M-series (parité Performance Page avec Intel) |
| **Unity Editor → Build Android APK** | ✅ | ✅ |
| **adb / sideload** | ✅ | ✅ |
| **OpenXR Runtime Quest sur device** | ✅ | ✅ |

**Conclusion vérifiée** : Apple Silicon est officiellement supporté par Meta sur le toolchain Unity ; Link reste Windows-only et il n'y a pas de workaround. Le cycle d'itération macOS standard est :

```
[Unity Mac] → Build APK → adb push → run on Quest 3
              ↳ ou Meta XR Simulator (debug rapide UI/logique sans casque)
```

**Caveat honnête** ([source forum Meta + Stack Overflow 2024–2026](https://communityforums.atmeta.com/discussions/dev-unity/does-meta-xr-simulator-support-mac-os/1092455)) : des bugs sporadiques sur M2 Pro ont été signalés (`XR_ERROR_HANDLE_INVALID` sur certaines versions du Meta XR All-in-One v71). M4 Pro est plus récent et mieux supporté, mais prévoir un cycle **Build & Run sur device plus fréquent que sur Windows**.

**Verdict** : Mac M4 Pro est viable comme machine principale. Pas besoin d'acheter un PC. Le seul cas où un Windows serait clairement utile : iteration loops très fréquentes avec hand tracking en Play Mode (Windows + Link), mais c'est confort, pas blocage.

---

<a id="12-trio-mac-natif"></a>
### 1.2 Le trio Mac-natif : Simulator + MQDH + adb ✅ vérifié

#### Meta XR Simulator

Runtime OpenXR léger qui **simule un Quest depuis ta machine** (clavier/souris pour navigation, simulation de hand tracking, rooms MR de test). Pensé exactement pour réduire le besoin d'itérer en flashant l'APK.

- Package : `com.meta.xr.simulator` (Meta XR SDK v66+)
- Requirements : Unity OpenXR Plugin ≥ 1.13.0, Vulkan SDK, Meta XR SDK v66+
- Install : via Meta XR All-in-One SDK + Meta's [package Mac ARM v83.2](https://developers.meta.com/horizon/downloads/package/meta-xr-simulator-mac-arm/) ou via Homebrew formula officielle
- Doc : [developers.meta.com/horizon/documentation/unity/xrsim-intro](https://developers.meta.com/horizon/documentation/unity/xrsim-intro/)

> Citation Meta verbatim : *"Now with Mac support for Meta XR Simulator, you can easily iterate and test your projects without needing to deploy your app to your headset every time."*

**Limites honnêtes** :
- Perfs Synthetic Environment Server reconnues plus lentes que Windows par Meta elle-même
- Pas un substitut au test sur device (frame timing, thermals, comportements OS Quest)
- **Hand tracking dans Editor en Play Mode via Link** est explicitement **Windows-only** ([doc Meta](https://developers.meta.com/horizon/documentation/unity/unity-handtracking-overview/)) — sur Mac, le Simulator est la seule alternative pour tester hand tracking sans build

#### Meta Quest Developer Hub (MQDH)

App desktop officielle Meta. Natif Apple Silicon depuis 2024. Sert à :
- Sideload APK (drag & drop dans MQDH > `adb install` sous le capot)
- Capture vidéo/screenshots du casque
- Performance Page (FPS, GPU, CPU, thermals en live)
- Casting (mirror Quest 3 → écran Mac)
- File manager device

Download : [developer.oculus.com/downloads/package/oculus-developer-hub-mac/](https://developer.oculus.com/downloads/package/oculus-developer-hub-mac/)

#### adb (Android Debug Bridge)

Stock standard Android. Installé avec Android Studio ou directement via Homebrew :

```bash
brew install --cask android-platform-tools
adb devices                  # vérifier Quest connecté en USB
adb install path/to/build.apk
adb logcat -s Unity:V         # logs Unity en live
```

**Pour Ankhora** : ce trio couvre 100 % du workflow dev. Pas de besoin Windows.

---

<a id="13-cable-iniu"></a>
### 1.3 Câble INIU USB-A → USB-C avec dérivation chargeur

**Verdict** : ton câble fait le job pour le workflow Mac que tu vas adopter, mais il a des limites à connaître.

#### Ce qu'il faut au minimum pour dev Quest 3 sur Mac

| Usage | Spec requise | Câble INIU OK ? |
|---|---|---|
| `adb push` APK 50–500 MB | USB 2.0 suffit (~35 MB/s) | ✅ |
| `adb logcat` streaming | USB 2.0 OK | ✅ |
| MQDH casting | USB 2.0 OK pour basse résolution | ✅ |
| Charge pendant dev long | 5W min, idéalement 18W+ | ✅ avec dérivation chargeur |
| **Quest Link / Air Link** | USB 3.0+ et Windows | ❌ N/A — Link Windows-only |

#### Le piège USB-A vs USB-C

Ton Mac M4 Pro n'a **que des ports USB-C** (Thunderbolt/USB4). Un câble USB-A → USB-C **ne se branche pas directement** sur le Mac. Tu as deux options :

1. **Adaptateur USB-A femelle → USB-C mâle** (5–10 €). Marche, mais bridé USB 2.0 si l'adaptateur est cheap.
2. **Câble dédié USB-C → USB-C** côté Mac (recommandé pour pérennité). Pour Quest 3 Mac dev, n'importe quel câble USB-C ↔ USB-C data + power suffit.

#### Ce qu'utilisent les studios pro

- **Câble officiel Meta Quest Link Cable** (~80 €, fibre optique 5 m, USB 3.2 Gen 1) — overkill pour Mac
- **Anker PowerLine III USB-C ↔ USB-C 10 Gbps 3 m** (~25 €) — sweet spot Mac/PC, marche pour Link aussi
- **KIWI design Link Cable** (~30 €) — populaire chez devs Quest, USB 3.0, 5 m

**Recommandation Ankhora** : tu peux continuer avec ton INIU + adaptateur tant que tu n'as pas besoin de Link. Quand tu auras envie de tester un workflow Link (potentiellement sur un PC emprunté), prends un **Anker USB-C ↔ USB-C 3 m USB 3.2 Gen 1** (~25 €).

> ⚠️ Source non vérifiée dans la passe deep-research : claims précis sur débits Link et USB-C — à recouper si tu vises Quest Link plus tard.

---

<a id="14-setup-unity-step-by-step"></a>
### 1.4 Setup Unity step-by-step pour Quest 3 sur Mac

#### Versions recommandées (mai 2026)

| Composant | Version recommandée | Pourquoi |
|---|---|---|
| **Unity Editor** | **Unity 6 LTS** (6000.0.x) ou **2022.3 LTS** | Unity 6 = nouveau LTS, support OpenXR et URP modernisé. 2022.3 LTS reste valide si tu veux maximiser la compat avec le repo `mr-example-meta-openxr` (Unity 2022.3.8f1) |
| **Render Pipeline** | **URP** (Universal RP) | Quest 3 est mobile GPU. Built-in est legacy, HDRP n'est pas viable mobile |
| **Backend de scripting** | **IL2CPP** | Obligatoire pour Quest (Mono pas supporté) |
| **Target architecture** | **ARM64** uniquement | Quest 3 = Snapdragon XR2 Gen 2 ARM64 |
| **Min API Level** | **Android 12 (API 32)** | Aligné Meta Quest OS actuel |
| **Color space** | **Linear** | Required pour passthrough qualité |
| **Multiview rendering** | **Multiview** ou **Single Pass Instanced** | Perf VR critique |

#### Packages obligatoires (ordre exact d'installation)

1. **Android Build Support module** (via Unity Hub > Installs > ⚙️ > Add Modules) :
   - Android SDK & NDK Tools
   - OpenJDK
2. **OpenXR Plugin** (`com.unity.xr.openxr`) — via Package Manager
3. **XR Plugin Management** (`com.unity.xr.management`)
4. **Meta XR All-in-One SDK** — via [Asset Store](https://assetstore.unity.com/packages/tools/integration/meta-xr-all-in-one-sdk-269657) (gratuit) ou Meta site direct
   - Inclut : Core SDK, Interaction SDK, Voice SDK, Platform SDK, Audio SDK
5. **XR Interaction Toolkit 3.x** (`com.unity.xr.interaction.toolkit`)
6. **XR Hands** (`com.unity.xr.hands`) — pour ghost hands via `HandVisualizer`
7. **AR Foundation** (`com.unity.xr.arfoundation`) — pour plane detection / passthrough MR
8. **Meta-OpenXR** (`com.unity.xr.meta-openxr`) — bridge entre OpenXR et features Meta

> ⚠️ Les versions exactes des packages évoluent vite (Meta SDK v66 → v83+ en 18 mois). **Toujours résoudre la version actuelle via Package Manager au moment du setup**, ou via le MCP `context7` qui a la doc à jour.

#### Configuration via Building Blocks + Project Setup Tool ✅ vérifié

**C'est le levier d'efficacité #1 pour un dev solo.**

> Citation Meta verbatim : *"Building Blocks is a developer tool for Unity, that helps you quickly start building a new Meta Quest app or add features to your existing project. Each Building Block represents an atomic piece of Meta Quest functionality. After adding a Building Block to a scene, all of the feature's dependencies are installed automatically for you. Any required configuration is also done automatically with the Project Setup Tool."*

Workflow :
1. `Meta > Tools > Building Blocks`
2. Drag les blocks dont tu as besoin dans la scène (Camera Rig, Passthrough, Hand Tracking, Spatial Anchor, etc.)
3. Le Project Setup Tool corrige automatiquement Player Settings, XR Plugin config, Manifests Android, permissions

Doc : [developers.meta.com/horizon/documentation/unity/bb-overview](https://developers.meta.com/horizon/documentation/unity/bb-overview/)

#### Player Settings critiques Quest 3

| Setting | Valeur |
|---|---|
| Platform | Android |
| Texture Compression | ASTC |
| Active Input Handling | Both ou New Input System |
| Color Space | Linear |
| Graphics APIs | **Vulkan** uniquement (retirer OpenGLES) |
| Multithreaded Rendering | ✅ |
| Use 32-bit Display Buffer | ❌ |
| Disable Depth and Stencil | ❌ (besoin pour passthrough) |
| Scripting Backend | IL2CPP |
| Target Architectures | ARM64 only |
| Optimized Frame Pacing | ✅ |

#### Gotchas Apple Silicon

- Si bug `Failed to load openxr runtime loader` avec Simulator : vérifier que tu es bien sur le package **Mac ARM** (pas Intel), Unity OpenXR Plugin ≥ 1.13.0
- Unity Editor Apple Silicon native depuis Unity 2022.2 — toujours prendre la version "Apple silicon" dans Unity Hub
- Android SDK installé via Unity Hub a parfois des soucis de path — si problème, `~/Library/Android/sdk` doit être lisible par Unity (chmod si besoin)

---

<a id="15-features-xr-critiques"></a>
### 1.5 Features XR critiques pour Ankhora ✅ vérifié

#### Hand tracking → ghost hands ✅

**Recommandation Meta officielle** : utiliser l'**Interaction SDK** (livré dans Meta XR All-in-One). Implémenter des interactions custom **sans** ce SDK *"makes it difficult to get approved in the store"* — citation Meta verbatim. Pour Ankhora qui vise le Quest Store, c'est rédhibitoire.

**Pour les ghost hands** (vue learner du mouvement de l'expert) :
- `HandVisualizer` (package `com.unity.xr.hands`) — open, simple, mesh skeleton standard
- `OVRHand` + `OVRSkeleton` (Core SDK) — plus de features Meta-spécifiques

**Capacités hand tracking utiles pour Ankhora** :
- **Fast Motion Mode** (60 Hz) : capter geste expert rapide (manipulation outil, geste culinaire)
- **Wide Motion Mode** : capter mains qui sortent du champ de vue (geste large bras tendu)
- **Multimodal** : mains + controllers en simultané
- **Capsense** : pose mains logique même controller en main

Doc : [developers.meta.com/horizon/documentation/unity/unity-handtracking-overview](https://developers.meta.com/horizon/documentation/unity/unity-handtracking-overview/)

> ⚠️ Sur **Mac**, tester le hand tracking en Play Mode Editor n'est **pas** possible via Link. Tu as deux options : (1) **Meta XR Simulator** (simulation), (2) **build & run sur device** à chaque itération.

#### Spatial anchors (persistance cross-session) ✅

**C'est l'API qui sous-tend directement "masterclass spatialisée dans un atelier réel réutilisable".**

> Citation Meta : *"An anchor is a world-locked frame of reference. Save the anchor to persist it across sessions. In later sessions, or when using a large space like an entire home, you can query and find the persisted anchors."*

**Limites documentées** :
- Enhanced Spatial Services requis (Quest 3 ✅)
- Qualité tracking dégradée >3 m de l'anchor (bind range)
- Cap ~200 m² physique et ~15 pièces stockables on-device
- Multi-room space discovery supporté depuis SDK v66+

**Pour Ankhora** : un anchor par "station de masterclass" dans l'atelier (par exemple : station 1 = poste de soudure, station 2 = poste mesure, etc.). Re-launch d'une masterclass localise l'anchor et superpose la scène 3D à la bonne position physique.

Doc : [developers.meta.com/horizon/documentation/unity/unity-spatial-anchors-overview](https://developers.meta.com/horizon/documentation/unity/unity-spatial-anchors-overview/)

#### Passthrough Quest 3 (Mixed Reality) ✅

Activation Unity :
1. `OVRManager > Quest Features > General > Passthrough Support` = `Required` (si MR obligatoire) ou `Supported` (si MR optionnel)
2. `Insight Passthrough > Enable Passthrough` (ou activer à runtime via `OVRManager.isInsightPassthroughEnabled`)

> ⚠️ Claim refutée 0-3 par le fact-check : *"il faut supprimer la Main Camera et utiliser OVRCameraRig"* — la doc Meta actuelle ne l'exige pas comme universel. Le rig dépend de ton setup (XRI standard ou OVR rig).

Doc : [developers.meta.com/horizon/documentation/unity/unity-passthrough-gs](https://developers.meta.com/horizon/documentation/unity/unity-passthrough-gs/)

#### Recording / replay de session XR (cœur du produit Ankhora) ⚠️ non couvert par deep-research

Aucune solution officielle Meta clé-en-main. Approches connues :

| Approche | Détails | Statut |
|---|---|---|
| **Custom serialization OVRSkeleton/XRHand** | Capturer transforms 25 joints/main + voix (Mic) + transform head à 30–60 Hz → binaire ou JSON timeline | DIY, ~1–2 semaines dev — c'est la voie réaliste pour le MVP |
| **Unity Recorder** | Capture vidéo/audio, pas data XR | Pas adapté pour replay interactif |
| **Plugins tiers (XRReplay, etc.)** | Marketplace immature, pas de leader open-source clair en 2026 | À surveiller |
| **Ultraleap / Manus capture** | Hardware externe, hors scope Quest standalone | N/A |

**Pour Ankhora** : custom est la bonne voie. Schema suggéré pour le MVP :

```json
{
  "version": 1,
  "duration_ms": 120000,
  "audio": "audio.opus",
  "head_track": [{ "t": 16, "pos": [x,y,z], "rot": [x,y,z,w] }, ...],
  "hands": {
    "left":  [{ "t": 16, "joints": [[x,y,z,qx,qy,qz,qw], ...x25] }, ...],
    "right": [...]
  },
  "anchors": [{ "id": "anchor-123", "world_pose": [...] }],
  "annotations": [
    { "t": 5000, "type": "text", "anchor_id": "anchor-123", "content": "..." },
    { "t": 12000, "type": "3d_shape", "kind": "arrow", "pose": [...] }
  ]
}
```

#### QR code launch ✅ vérifié

**Important** : ce n'est **PAS** un Android URI scheme classique.

> Citation Meta verbatim : *"The deeplink_message string is opaque to the platform layer — the originating app and target app agree on its format. The receiving app reads the string via GetLaunchDetails and routes accordingly. This is not an Android URI scheme. The platform layer does not register or route based on the string contents."*

**Architecture correcte pour Ankhora** :
1. Apprenant scanne un QR code (avec n'importe quelle app, ou la Quest camera)
2. Le QR encode quelque chose comme `meta-launch://AnkhoraApp?masterclass_id=abc123` **interprété par le launcher Meta**, qui appelle `Application.LaunchOtherApp("com.ankhora.app", deeplink_message="masterclass_id=abc123")`
3. Ankhora reçoit `Notification_ApplicationLifecycle_LaunchIntentChanged`
4. Lit `ApplicationLifecycle.GetLaunchDetails().DeeplinkMessage`
5. Parse `masterclass_id`, fetch contenu via API, lance la session

> ⚠️ Le détail exact du "trigger côté apprenant" (QR scanné via app companion mobile vs caméra Quest native vs SideQuest webview) **mérite un POC dédié** avant de finaliser l'architecture. Spec ouverte.

Doc : [developers.meta.com/horizon/documentation/unity/ps-deep-linking](https://developers.meta.com/horizon/documentation/unity/ps-deep-linking/)

#### Audio spatial ⚠️ non couvert par deep-research mais standard

Meta XR Audio SDK (livré dans All-in-One) — HRTF spatialisé, occlusion par anchors/géométrie. Pour Ankhora, c'est juste de la config Audio Source + Meta Spatializer plugin. Pas un sujet bloquant.

---

<a id="16-distribution"></a>
### 1.6 Distribution Quest 3 (Store, App Lab, side-loading, MDM)

> ⚠️ Cette section combine source vérifiée + connaissance générale. Confirmer les détails business avant tout commit produit.

#### Évolution App Lab → Meta Horizon Store

- **App Lab a fusionné** avec le main Quest Store sous le nom **Meta Horizon Store** (annonce Meta 2024). Doc : [developers.meta.com/horizon/blog/get-apps-ready-app-lab-meta-horizon-store-meta-quest-developers](https://developers.meta.com/horizon/blog/get-apps-ready-app-lab-meta-horizon-store-meta-quest-developers/)
- Plus de "App Lab" distinct — il y a un seul store avec différents niveaux de visibilité (catalogue principal vs lien direct)

#### Voies de distribution pour Ankhora

| Voie | Cible | Effort | Conditions |
|---|---|---|---|
| **Meta Horizon Store (main catalog)** | Public, B2C | ⭐⭐⭐⭐ | Review Meta strict, Interaction SDK obligatoire pour hand tracking |
| **Meta Horizon Store (lien direct)** | Beta testers, B2B early | ⭐⭐ | Review allégé, app accessible par URL |
| **Side-loading via MQDH / SideQuest** | Dev / interne / clients pilotes | ⭐ | adb install — parfait pour MVP démo et clients pilotes |
| **Meta Quest for Business (MQfB)** | Enterprise B2B | ⭐⭐⭐ | Subscription Meta enterprise. Voir doc [developers.meta.com/horizon/resources/qfb-private-apps-dist](https://developers.meta.com/horizon/resources/qfb-private-apps-dist/) |
| **MDM ArborXR** | Déploiement fleet B2B | ⭐⭐⭐ | Payant, mais standard enterprise. Voir [arborxr.com/blog/how-to-use-kiosk-mode-with-meta-quest](https://arborxr.com/blog/how-to-use-kiosk-mode-with-meta-quest) |
| **MDM ManageXR** | Alternative ArborXR | ⭐⭐⭐ | Idem |

**Recommandation Ankhora MVP** :
- **Phase pilote** : side-loading via MQDH chez 1–3 clients (manufacturing, culinaire) — itération rapide, pas de blocage review
- **Phase scale** : Meta Quest for Business + ArborXR/ManageXR pour fleet management
- **Phase B2C masterclass marketplace** (V2+) : Horizon Store lien direct puis catalogue

#### Signature APK

Quest accepte les APK signés avec un keystore Android standard (`jarsigner` / `apksigner`). Pas de signing payant. **Backupper le keystore + password — perdu = tu ne peux plus publier d'update.**

---

<a id="axe-2"></a>
## 2. Tooling IA pour développement XR/Unity (AXE 2)

<a id="21-mcp-servers-unity"></a>
### 2.1 MCP servers Unity ✅ vérifié

Deux serveurs MCP Unity matures et actifs en mai 2026 :

#### CoplayDev/unity-mcp ⭐ recommandé pour Ankhora

- **Repo** : [github.com/CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp)
- **Stats** : 10 102 stars, MIT, dernier push 2026-05-27
- **Doc** : [docs.coplay.dev/coplay-mcp/guide](https://docs.coplay.dev/coplay-mcp/guide)
- **Capacités** : gérer assets, contrôler scènes, éditer scripts C#, lancer tests, automatiser workflows
- **Compatible** : Claude Code, Codex, Cursor, VS Code, Windsurf, LLMs locaux

> Citation projet : *"MCP for Unity bridges AI assistants — Claude, Codex, VS Code, local LLMs, and more — with your Unity Editor via the Model Context Protocol. Give your LLM the tools to manage assets, control scenes, edit scripts, run tests, and automate workflows."*

**Install** : suivre [docs.coplay.dev/coplay-mcp/guide](https://docs.coplay.dev/coplay-mcp/guide) — package Unity + config MCP côté Claude Code (`~/.claude/config.json` ou via `claude mcp add`).

#### CoderGamester/mcp-unity

- **Repo** : [github.com/CoderGamester/mcp-unity](https://github.com/CoderGamester/mcp-unity)
- **Stats** : ~1.7k stars
- **Particularité** : le repo contient déjà `CLAUDE.md`, `AGENTS.md` et `.windsurfrules` à la racine — **preuve d'intégration native avec ces agents**
- **Compatible** : Cursor, Claude Code, Codex, Windsurf

#### IvanMurzak/Unity-MCP (alternative)

- **Repo** : [github.com/IvanMurzak/Unity-MCP](https://github.com/IvanMurzak/Unity-MCP)
- Plus jeune, mais actif. À surveiller.

#### AnkleBreaker-Studio/unity-mcp-server

- **Repo** : [github.com/AnkleBreaker-Studio/unity-mcp-server](https://github.com/AnkleBreaker-Studio/unity-mcp-server)
- Approche serveur Node.js séparée. Niche.

**Verdict** : pour Ankhora, **commencer avec CoplayDev/unity-mcp** (le plus mature et actif). CoderGamester en backup si limitations.

<a id="22-mcp-blender"></a>
### 2.2 MCP Blender

#### ahujasid/blender-mcp ⭐ référence

- **Repo** : [github.com/ahujasid/blender-mcp](https://github.com/ahujasid/blender-mcp)
- Permet à Claude Code de manipuler Blender via MCP : créer objets, manipuler scène, exporter assets
- Pour ton apprentissage Blender et la création d'assets 3D Ankhora (props ateliers, modèles d'outils, etc.), c'est un game-changer
- Install : addon Blender + serveur MCP Python

<a id="23-rules-cursor-skills-cc"></a>
### 2.3 Rules Cursor / skills Claude Code pour Unity

> ⚠️ Section non systématiquement couverte par le deep-research — recommandations basées sur écosystème connu.

#### Cursor rules (.cursorrules ou .cursor/rules/)

Format Cursor moderne (2025+) : fichiers `.mdc` dans `.cursor/rules/`. Repos référence à cloner / inspirer :

- [github.com/PatrickJS/awesome-cursorrules](https://github.com/PatrickJS/awesome-cursorrules) — collection officieuse, section Unity/Game dev
- Communauté `awesome-mdc` (rules Cursor v3+) : recherche `cursor unity rules` sur GitHub Topics

**Templates Unity utiles à inclure** :
- C# coding standards Unity (PascalCase pour MonoBehaviour, [SerializeField] private prefer over public)
- Naming conventions assets (`{Type}_{Category}_{Name}` etc.)
- URP shader rules
- Meta XR SDK patterns (BB préféré aux setup manuels)
- Performance VR (90Hz mandatory budgets, batching, etc.)

#### Claude Code skills

Tu as déjà :
- `~/.claude/skills/` global (using-lsp, git-commit, deep-research, find-skills, etc.)
- Skills Vercel, Figma, Notion, Sentry, etc.

**Skills à créer pour Ankhora** :
1. `ankhora-unity-setup` — workflow exact pour configurer une scène (Meta XR rig + passthrough + hand tracking)
2. `ankhora-build-deploy` — build APK Mac + adb push + logcat
3. `ankhora-spec-check` — vérifie qu'un changement respecte les specs du dossier `docs/specs/`

Référence pour écrire des skills propres : `superpowers:writing-skills`, `skill-creator:skill-creator`.

<a id="24-subagents"></a>
### 2.4 Subagents Claude Code recommandés pour Ankhora

Tu as déjà beaucoup d'agents configurés. Les plus utiles pour ce projet :

| Subagent | Usage Ankhora |
|---|---|
| `Explore` | exploration codebase Unity (3+ queries) → mapping de scripts, MonoBehaviours, scenes |
| `Plan` | architecture de features XR avant code (recording layer, spatial anchor management, deep link routing) |
| `feature-dev:code-explorer` | analyser features existantes (ex: pattern Meta MR Example) avant de t'inspirer |
| `feature-dev:code-architect` | design d'une feature complète (ex: système d'annotations 3D) |
| `feature-dev:code-reviewer` | review focused des PRs/commits Ankhora |
| `coderabbit:code-reviewer` | review automatique IA |
| `librarian` | requêter ton vault Brain pour décisions/concepts archivés |
| `episodic-memory:search-conversations` | retrouver "comment j'avais résolu X la fois passée" |

**Anti-pattern à éviter** : ne pas spawner d'agents pour des tâches que tu peux faire en 1 grep. Les agents coûtent du temps et des tokens — utiliser quand le boulot est vraiment indépendant et parallélisable.

<a id="25-ai-natifs-unity"></a>
### 2.5 AI natifs Unity (Muse, Sentis)

| Outil | Statut 2026 | Pertinence Ankhora |
|---|---|---|
| **Unity Muse** | Subscription Unity Pro (~$30/mo) — Chat, Sprite, Texture, Animation, Behavior | ❌ Hors budget MVP. Cursor/Claude Code couvrent largement |
| **Unity Sentis** | Runtime ONNX dans Unity — **gratuit**, intéressant si tu veux embarquer un petit LLM ou un modèle ML on-device | ✅ Potentiel V2 — fallback chatbot RAG offline / classification gestes apprenant |
| **Unity Cloud** | Asset Manager, Build automation, DevOps | Voir 2.6 |

**Verdict** : tu n'as pas besoin de Muse. Sentis est à surveiller pour V2 (ex : détection d'erreur de geste apprenant via un classifier ONNX).

<a id="26-stack-complementaire"></a>
### 2.6 Stack complémentaire utilisée par les studios

| Catégorie | Outil | Gratuit ? | Recommandation Ankhora |
|---|---|---|---|
| **Source control** | Git + Git LFS | ✅ | ⭐ Standard. LFS pour assets binaires Unity |
| | Plastic SCM / Unity Version Control | Free up to 3 users + 5 GB | Alternative si projet Unity-natif lourd |
| | Perforce | $$$ | Standard studio AAA — pas pour solo |
| **Project management** | Linear | Free up to 250 issues | ⭐ Si tu veux structurer le backlog |
| | Notion | Free perso | ⭐ Tu l'utilises déjà via MCP — parfait pour spec + roadmap |
| | GitHub Projects | ✅ avec repo | Suffit pour solo |
| **CI/CD** | **GameCI** (GitHub Actions) | ✅ OSS | ⭐⭐⭐ Standard pour Unity OSS. Doc : [game.ci/docs/github/getting-started](https://game.ci/docs/github/getting-started/) |
| | Unity Cloud Build | Payant | Trop cher pour solo |
| | GitHub Actions Mac runners | ✅ (limites free tier) | Build Android Quest faisable depuis Mac runner |
| **Profiling / debug** | Unity Profiler | ✅ Bundled | Standard |
| | OVR Metrics Tool | ✅ App Quest | ⭐ Sur device. Pour vérifier 72/90/120 Hz, GPU/CPU |
| | RenderDoc Meta | ✅ | Debug frame-level GPU |
| | Meta XR Simulator | ✅ Bundled SDK | Voir 1.2 |
| **3D assets** | Sketchfab | Free + payant | Modèles d'outils/objets |
| | Poly Haven | ✅ CC0 | HDRIs, textures, modèles libres CC0 |
| | Quixel Megascans | ✅ free with Epic ID | Photoréaliste, gratuit avec compte Epic |
| | Mixamo | ✅ Adobe | Personnages riggés/animés gratuits |
| | Ready Player Me | ✅ tier free | Avatars utilisateurs (potentiel V2 multiplayer) |
| **DCC** | Blender | ✅ | Déjà ta stack ⭐ |
| | Substance | $$$ | Pas pour MVP |
| | Marmoset Toolbag | $$$ | Pas pour MVP |
| **Audio** | Audacity | ✅ | Édition audio voix masterclass |
| | Reaper | $60 perso | DAW si besoin audio complexe |
| | Meta XR Audio SDK | ✅ Bundled | Spatialisation 3D |
| **Communauté / docs** | r/OculusDev, r/Unity3D | ✅ | Veille |
| | discussions.unity.com | ✅ | Q&A officiel |
| | communityforums.atmeta.com | ✅ | Forums officiels Meta dev |
| | Unity XR Discord | ✅ | Real-time help |

---

<a id="axe-3"></a>
## 3. Spec-first development pour piloter le LLM (AXE 3)

> Cette section est **critique pour ton focus actuel** : tu ne veux pas coder tout de suite, tu veux d'abord poser une documentation solide qui orientera Claude Code/Cursor pendant tout le dev.

<a id="31-spec-frameworks"></a>
### 3.1 Comparatif Spec-Kit / OpenSpec / cc-sdd

#### GitHub Spec-Kit ⭐ le plus mature

- **Repo** : [github.com/github/spec-kit](https://github.com/github/spec-kit)
- **Mainteneur** : GitHub officiel
- **Philosophie** : Spec-Driven Development (SDD). Le code devient un artefact, la spec devient la source de vérité. Cycle : `/specify` → `/plan` → `/tasks` → `/implement`
- **Intégrations** : Claude Code, GitHub Copilot, Cursor, Gemini CLI, OpenCode, Windsurf, Qwen Code, opencode, Codex CLI, Kilo Code, Auggie CLI, Roo Code, Codebuddy, Q Developer
- **Outil** : `specify` CLI Python (`uvx specify-cli`)
- **Statut** : projet officiel GitHub, traction forte 2025–2026

**Workflow** :
1. `specify init` dans le repo
2. Tu écris une spec haut niveau dans `spec/`
3. Le CLI génère un plan d'implémentation
4. Le plan génère des tâches granulaires
5. Le LLM (Claude Code) implémente tâche par tâche en se référant à la spec

#### OpenSpec — alternative légère

- **Repo** : [github.com/Fission-AI/OpenSpec](https://github.com/Fission-AI/OpenSpec)
- Plus simple, moins prescriptif que Spec-Kit
- Bon si tu veux un cadre léger sans la chorégraphie GitHub

#### cc-sdd (gotalab) — Kiro-style sur Claude Code

- **Repo** : [github.com/gotalab/cc-sdd](https://github.com/gotalab/cc-sdd)
- Apporte le pattern Kiro (Amazon Spec-Driven Dev) à Claude Code
- Plus opinionated, scaffold complet

#### Tableau comparatif

| Critère | Spec-Kit | OpenSpec | cc-sdd |
|---|---|---|---|
| Mainteneur | GitHub officiel | Fission-AI (communauté) | gotalab (communauté) |
| Maturité | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ |
| Intégration Claude Code | ✅ Native | ✅ | ✅ Native |
| Intégration Cursor | ✅ | ✅ | ⚠️ via .mdc |
| Verbosité spec | Élevée (PRD complet) | Moyenne | Élevée |
| Courbe d'apprentissage | Moyenne | Faible | Moyenne |
| Pour Ankhora | ⭐ Top choix | Backup léger | Si tu veux Kiro-style |

**Verdict Ankhora** : **Spec-Kit en priorité**. Tu auras un cadre clair, des prompts standardisés, et l'écosystème GitHub te donne traction long terme. Si trop lourd → fallback OpenSpec.

<a id="32-ai-native-methodo"></a>
### 3.2 Méthodologie AI-native spec-driven

#### Le tournant "the new code" (Sean Grove, OpenAI)

L'argument : les specs sont *le code* de demain. Le LLM est l'exécuteur. Si ta spec est ambiguë, ton code est mauvais — peu importe le modèle.

Principes durables :
- **Specs > prompts ad-hoc** : passe ton temps à écrire la spec, pas à reformuler tes prompts
- **Specs versionnées** dans Git, reviewables, diffables
- **Specs testables** : chaque section doit pouvoir produire des tests
- **Specs accessibles au LLM en permanence** via `CLAUDE.md` et `.cursor/rules/`

#### LLM Wiki (Karpathy)

Karpathy a popularisé le pattern "wiki que ton LLM peut requêter". Tu l'utilises déjà avec ton `Brain/` vault et l'agent `librarian`. Pour Ankhora, dupliquer ce pattern :

```
Ankhora/
├── docs/                    # specs versionnées (PRD, archi, ADR, glossaire)
├── .claude/                 # rules, skills, agents projet-spécifiques
│   └── CLAUDE.md            # surcharge globale au scope projet
├── .cursor/
│   └── rules/               # rules Cursor (.mdc)
└── CLAUDE.md                # racine — pointe vers docs/ et conventions
```

#### PRD-driven workflow (Anthropic, communauté)

Anthropic ne publie pas (encore) de guide officiel, mais l'écosystème converge sur :
1. **PRD** (Product Requirements Document) — pourquoi + quoi
2. **Tech Spec** — comment (architecture, choix techniques)
3. **Implementation Plan** — découpe en tâches
4. **CLAUDE.md** — règles permanentes du projet
5. **ADR** (Architecture Decision Records) — décisions importantes archivées
6. **Glossaire** — termes domaine (masterclass, ghost hand, anchor, etc.)

<a id="33-structure-docs"></a>
### 3.3 Structure docs/specs recommandée pour Ankhora

```
Ankhora/
├── CLAUDE.md                         # règles permanentes + pointeurs
├── README.md                         # entry point humain
│
├── docs/
│   ├── 00-vision.md                  # pourquoi Ankhora existe (TL;DR 1 page)
│   ├── 01-product/
│   │   ├── prd.md                    # PRD complet
│   │   ├── personas.md               # expert créateur, apprenant, admin entreprise
│   │   ├── user-stories.md           # epics + stories MVP
│   │   ├── mvp-scope.md              # what's in / what's out (section 3.4 ci-dessous)
│   │   └── competitive-analysis.md   # vs Strivr, Mursion, Talespin, etc.
│   │
│   ├── 02-architecture/
│   │   ├── tech-stack.md             # Unity 6 LTS + Meta XR SDK + ...
│   │   ├── system-overview.md        # diagramme blocs (Quest app, web app, RAG, marketplace)
│   │   ├── data-model.md             # masterclass schema, recording schema
│   │   ├── api-contracts.md          # endpoints web ↔ Quest, RAG ↔ Quest
│   │   └── adr/
│   │       ├── 0001-unity-6-lts.md
│   │       ├── 0002-mac-as-primary-dev.md
│   │       ├── 0003-spec-kit-as-spec-framework.md
│   │       └── ...
│   │
│   ├── 03-xr/
│   │   ├── recording-system.md       # capture voix + hand tracking + annotations
│   │   ├── playback-system.md        # rejeu masterclass, ghost hands
│   │   ├── spatial-anchors.md        # gestion stations / persistance
│   │   ├── passthrough-mr.md         # config MR Quest 3
│   │   ├── deep-linking-qr.md        # QR → launch flow
│   │   └── hand-tracking-strategy.md # Interaction SDK + ghost hands rendering
│   │
│   ├── 04-web-platform/              # tout ce qui est V2 mais à esquisser
│   │   ├── rag-chatbot.md            # config sources, deployment
│   │   ├── marketplace.md            # listing, paiement, distribution
│   │   └── admin-dashboard.md
│   │
│   ├── 05-operations/
│   │   ├── build-deploy.md           # build APK Mac, adb, MQDH, distribution
│   │   ├── testing-strategy.md       # tests Unity, fact-check manuel device
│   │   ├── analytics.md              # quels événements tracker
│   │   └── enterprise-deployment.md  # ArborXR / ManageXR / MQfB
│   │
│   ├── 06-glossary.md                # vocabulaire produit + technique
│   ├── 07-milestones.md              # roadmap court terme
│   └── 08-research/                  # ce dossier — recherches archivées
│       └── xr-platform-master-research.md   # ce fichier
│
├── .claude/
│   ├── CLAUDE.md                     # règles projet-spécifiques (override global)
│   ├── skills/                       # skills Ankhora
│   │   ├── ankhora-unity-setup/
│   │   ├── ankhora-build-deploy/
│   │   └── ankhora-spec-check/
│   ├── agents/                       # subagents projet-spécifiques si besoin
│   └── settings.json                 # hooks, perms
│
├── .cursor/
│   └── rules/
│       ├── 001-unity-conventions.mdc
│       ├── 002-meta-xr-sdk.mdc
│       ├── 003-vr-performance.mdc
│       └── 004-ankhora-domain.mdc
│
├── Assets/
├── ProjectSettings/
└── ...
```

#### CLAUDE.md racine — squelette suggéré

```markdown
# Ankhora — Project Context

> Plateforme XR enterprise pour masterclasses immersives sur Meta Quest 3.

## What is Ankhora
[1–2 lignes from docs/00-vision.md]

## Tech stack
- Unity 6 LTS, URP, IL2CPP, ARM64, Vulkan
- Meta XR All-in-One SDK (Core + Interaction + Platform)
- OpenXR Plugin + XR Hands + AR Foundation + Meta-OpenXR
- Target: Meta Quest 3 standalone

## Dev environment
- Mac M4 Pro primary (no Quest Link — use Build & Run + Meta XR Simulator)
- See docs/05-operations/build-deploy.md for exact commands

## Conventions
- C# naming: PascalCase for types, camelCase for fields, _camelCase for private SerializeField
- All XR features go through Meta Building Blocks first (no manual rigging unless documented in ADR)
- Hand tracking interactions: Interaction SDK only (Meta requirement for store)
- All decisions of consequence go in docs/02-architecture/adr/

## Always check before coding
1. Read docs/01-product/mvp-scope.md to know if it's MVP or V2
2. Read related docs/03-xr/*.md for the feature
3. Check docs/02-architecture/adr/ for prior decisions

## Never
- Never use OpenGL ES (Vulkan only)
- Never push directly to main (use feature branches)
- Never commit Library/ Temp/ Logs/ ProjectSettings/Packages-lock.json contention
- Never bypass Building Blocks for features Meta supports natively

## References
- docs/08-research/xr-platform-master-research.md — full research dossier
- ~/.claude/CLAUDE.md — global Lény ops manual
```

<a id="34-mvp-v2"></a>
### 3.4 Découpe MVP vs V2 argumentée

> Tu as **peu de temps** et tu es **seul**. Le piège mortel : viser le produit complet décrit dans la vision (XR + web + RAG + marketplace + multi-industries). C'est 12–18 mois solo. Découpe agressive.

#### MVP — 3 mois — "Une masterclass enregistrée par un expert, rejouée par un apprenant"

**Inclus** :

✅ App Quest 3 standalone Unity
✅ Mode `Record` : un expert enregistre voix + transforms head/hands + texte annotations + 1–2 formes 3D simples (flèche, sphère)
✅ Persistance : la masterclass est sauvée dans un fichier JSON+audio sur device, **uploadable manuellement** vers un backend simple
✅ Mode `Play` : un apprenant load une masterclass, voit les ghost hands, écoute la voix
✅ 1 spatial anchor par masterclass (pas multi-room)
✅ Passthrough MR activable
✅ Distribution : **side-loading via MQDH** chez 1–2 clients pilotes
✅ Web minimal : une page upload + listing simple (Next.js ou même statique)

**Exclus du MVP — V2** :

❌ RAG chatbot in-headset (gros chantier — embeddings, vector DB, UI in-VR)
❌ Marketplace public + paiement
❌ QR code launcher (custom flow, complexe — utiliser launch direct depuis menu Quest pour MVP)
❌ Multi-utilisateurs simultanés / colocated
❌ MDM enterprise (ArborXR/ManageXR) — pas avant déploiement fleet
❌ Multi-industries customizations
❌ Recording de vidéos d'expert intégrées (juste annotations + ghost hands suffit)
❌ Analytics avancées

#### Pourquoi cette découpe

1. **Reduce risk on the critical 20%** : le hand tracking + ghost hands + persistance d'anchors + replay synchronisé est *déjà* un défi technique. Si ça ne marche pas, le reste ne sert à rien.
2. **Show, don't tell** : un MVP rejouable chez un client pilote prouve la vision et ouvre les portes (financement, premiers paying users, validation marché).
3. **Le RAG chatbot peut attendre** : tu as déjà l'expertise Tolk.ai/Genii. Tu peux brancher Genii après que le core XR marche.

#### Critères pour décider qu'une feature passe en V2

- Demande >2 semaines de dev solo
- Bloque pas la démo client pilote
- Pas dans la promesse de valeur "rejouer une masterclass"
- Dépend d'infrastructure non livrée (paiement, MDM, multi-tenant)

<a id="35-ressources"></a>
### 3.5 Ressources d'apprentissage

#### YouTube (priorité tutoriels < 18 mois)

| Créateur | URL | Pertinence Ankhora |
|---|---|---|
| **ValemTutorials** | [@ValemTutorials](https://www.youtube.com/@ValemTutorials) | ⭐⭐⭐⭐ Excellents tutos Unity XR / Quest 3. Tu l'as déjà repéré. Priorité |
| **Justin P. Barnett** | [@justinpbarnett](https://www.youtube.com/@justinpbarnett) | ⭐⭐⭐⭐ Très bonne pédagogie XR Unity, Quest 3 setup, hand tracking |
| **Dilmer Valecillos** | [@dilmerv](https://www.youtube.com/@dilmerv) | ⭐⭐⭐⭐ Meta XR SDK deep dives, spatial anchors, MR. Référence |
| **Daniel Buckley** | [@DanielBuckleyAR](https://www.youtube.com/@DanielBuckleyAR) | ⭐⭐⭐ AR-focus mais bons concepts |
| **Black Whale** | [@BlackWhaleStudio](https://www.youtube.com/@BlackWhaleStudio) | ⭐⭐⭐ Production VR Unity, conseils studio |
| **Unity** | [@Unity](https://www.youtube.com/@unity) | ⭐⭐⭐ Officiel, Unite talks XR |
| **Meta Developers** | [@MetaDevelopers](https://www.youtube.com/@MetaDevelopers) | ⭐⭐⭐ Connect/Unite recap, deep dives Meta XR SDK |

#### Documentation officielle (à mettre en bookmarks)

| Source | URL | Usage |
|---|---|---|
| Meta Horizon Dev Docs | [developers.meta.com/horizon/documentation/unity](https://developers.meta.com/horizon/documentation/unity/) | Référence #1 |
| Unity Manual XR | [docs.unity3d.com/Manual/XR.html](https://docs.unity3d.com/Manual/XR.html) | OpenXR, XRI, XR Hands |
| Unity XR Hands | [docs.unity3d.com/Packages/com.unity.xr.hands@latest](https://docs.unity3d.com/Packages/com.unity.xr.hands@latest/manual/index.html) | HandVisualizer pour ghost hands |
| OpenXR spec | [registry.khronos.org/OpenXR](https://registry.khronos.org/OpenXR/) | Référence bas niveau |
| Unity-Technologies/mr-example-meta-openxr | [github.com/Unity-Technologies/mr-example-meta-openxr](https://github.com/Unity-Technologies/mr-example-meta-openxr) | ⭐⭐⭐⭐ **Repo référence à cloner et étudier** |

#### Cours payants (note seulement si tu changes d'avis sur le budget)

- Coursera "Introduction to XR" (Michigan) — bases
- Pluralsight Unity XR paths — propre mais cher
- VR-pro courses (Justin P. Barnett, ValemTutorials premium)

#### Communautés

- **Meta Quest Dev Discord** (official invite via developers.meta.com)
- **Unity XR Discord**
- **r/OculusDev** (Reddit)
- **r/Unity3D**

---

<a id="plan-2-semaines"></a>
## 4. Plan d'action 2 semaines (avant tout dev "feature")

> Objectif : à la fin de ces 2 semaines, tu as un environnement Mac→Quest fonctionnel, une doc de specs solide, et tu es prêt à coder en sachant pourquoi tu codes ce que tu codes.

### Semaine 1 — Setup machine + apprentissage actif

#### Jour 1 — Environnement de base (1/2 journée)
- [ ] Installer **Unity Hub** + **Unity 6 LTS** Apple Silicon avec Android Build Support, SDK, NDK, OpenJDK
- [ ] Installer **adb** : `brew install --cask android-platform-tools`
- [ ] Installer **Meta Quest Developer Hub (MQDH)** macOS
- [ ] Activer Developer Mode sur ton Quest 3 (app companion Meta Quest mobile > Casque > Mode développeur)
- [ ] Brancher Quest via INIU + adaptateur USB-A femelle → USB-C mâle → Mac
- [ ] `adb devices` → vérifier Quest visible. Si non, accepter le prompt USB debug dans le casque
- [ ] Tester un APK arbitraire (n'importe quel `.apk` Quest open-source) : `adb install foo.apk`

#### Jour 2 — Premier projet Unity Quest hello world (1 journée)
- [ ] Cloner **Unity-Technologies/mr-example-meta-openxr** → ouvrir dans Unity
- [ ] Builder pour Android Quest depuis ton Mac
- [ ] Push APK via MQDH ou `adb install`
- [ ] Lancer sur le casque → vérifier passthrough + hand tracking fonctionnent
- [ ] **Lire le README du repo** + naviguer dans le code des scènes Coaching / Spatial UI
- [ ] **C'est l'étalon de référence Ankhora**

#### Jour 3 — Meta XR Simulator + tooling Mac (1 journée)
- [ ] Installer **Meta XR Simulator** (Mac ARM) via Homebrew ou direct download
- [ ] Lancer le projet `mr-example-meta-openxr` dans le Simulator → vérifier que ça tourne
- [ ] Installer **OVR Metrics Tool** sur Quest pour monitoring perf
- [ ] Tester un cycle complet : modifier code → build APK Mac → push → run

#### Jour 4 — MCP Unity + AI tooling (1 journée)
- [ ] Cloner et installer **CoplayDev/unity-mcp**
- [ ] Configurer Claude Code pour s'y connecter (`claude mcp add`)
- [ ] Tester : demander à Claude Code de lister les scènes du projet `mr-example`, ouvrir une scène, lister les MonoBehaviours
- [ ] Installer **ahujasid/blender-mcp** si Blender est ouvert régulièrement
- [ ] Bookmarker context7 MCP, deepwiki MCP — utiles pour requêter doc Meta/Unity à la volée

#### Jour 5 — Tutos vidéos focused (1 journée)
- [ ] ValemTutorials playlist "VR Development with Unity 6" (récent)
- [ ] Justin P. Barnett "Meta Quest 3 Unity Setup 2025/2026"
- [ ] Dilmer Valecillos : 1 vidéo sur Spatial Anchors + 1 sur Hand Tracking
- [ ] Prendre des notes brutes dans `docs/08-research/learning-notes.md`

#### Jour 6–7 — Spec foundation (week-end ou 1.5 jour)
- [ ] Installer **GitHub Spec-Kit** : `uvx specify-cli init`
- [ ] Créer la structure de `docs/` listée en 3.3
- [ ] Écrire `docs/00-vision.md` (1 page, max)
- [ ] Écrire `docs/01-product/personas.md` (3 personas : expert créateur, apprenant, admin)
- [ ] Écrire `docs/01-product/mvp-scope.md` en t'inspirant de 3.4
- [ ] Écrire `CLAUDE.md` racine en t'inspirant du squelette 3.3

### Semaine 2 — Specs techniques + premier POC ciblé

#### Jour 8 — Tech stack + ADRs (1 journée)
- [ ] `docs/02-architecture/tech-stack.md` détaillé (versions, packages)
- [ ] ADR 0001 : `unity-6-lts.md` (décision et justification)
- [ ] ADR 0002 : `mac-as-primary-dev.md`
- [ ] ADR 0003 : `spec-kit-as-spec-framework.md`
- [ ] ADR 0004 : `meta-xr-all-in-one-sdk.md`
- [ ] ADR 0005 : `interaction-sdk-for-hand-tracking.md` (avec citation Meta sur store approval)

#### Jour 9 — Specs XR cœur (1 journée)
- [ ] `docs/03-xr/recording-system.md` (schéma JSON + flow capture)
- [ ] `docs/03-xr/playback-system.md` (deserialization + ghost hand rendering)
- [ ] `docs/03-xr/spatial-anchors.md` (limites, gestion 1 anchor/masterclass MVP)
- [ ] `docs/03-xr/hand-tracking-strategy.md` (Interaction SDK + HandVisualizer/OVRHand)

#### Jour 10 — Specs ops + glossaire (1/2 journée)
- [ ] `docs/05-operations/build-deploy.md` (commandes exactes Mac → Quest)
- [ ] `docs/05-operations/testing-strategy.md` (manuel device, Simulator pour logique pure)
- [ ] `docs/06-glossary.md` (masterclass, ghost hands, anchor, station, apprenant, expert, etc.)

#### Jour 11 — POC #1 : "Hello hand tracking" custom (1 journée)
- [ ] Nouveau projet Unity Ankhora vierge
- [ ] Setup minimal : Camera Rig + Hand Tracking via Building Blocks
- [ ] Scène avec 2 mains qui apparaissent en passthrough
- [ ] Cube qu'on peut grab
- [ ] Build APK → run sur device

#### Jour 12 — POC #2 : "Record + replay hand transforms" (1 journée)
- [ ] Étend POC #1 : bouton (UI 3D ou gesture) qui démarre/stop un recording
- [ ] Sérialiser positions/rotations des deux mains pendant 30 sec dans un JSON sur device
- [ ] Bouton replay : lire le JSON, déplacer 2 "ghost hands" semi-transparentes selon timeline
- [ ] **C'est le proof of value technique Ankhora**

#### Jour 13 — POC #3 : "Spatial anchor + replay localisé" (1 journée)
- [ ] Bouton qui pose un Spatial Anchor à la position des mains
- [ ] Sauvegarder l'anchor uuid + le recording lié
- [ ] Au launch, query anchors persistés, charger le recording associé, replay ghost hands à la bonne position spatiale

#### Jour 14 — Bilan + plan dev MVP (1 journée)
- [ ] Tester les 3 POCs sur device, noter ce qui marche / ne marche pas
- [ ] Mettre à jour `mvp-scope.md` selon retour réalité
- [ ] Découper le MVP en sprints de 1 semaine avec Spec-Kit `/plan` et `/tasks`
- [ ] Préparer 1ère démo orale pour Tolk.ai (vision + POCs)

---

<a id="annexes"></a>
## 5. Annexes

### A. Sources principales (33 fetched, ranked)

#### Primary (Meta, Unity, GitHub officiels)
- [developers.meta.com/horizon/blog/mac-support-unity-meta-quest-horizon-developer](https://developers.meta.com/horizon/blog/mac-support-unity-meta-quest-horizon-developer/)
- [developers.meta.com/horizon/blog/meta-xr-simulator-pc-mac-unity-unreal-developer-mixed-reality](https://developers.meta.com/horizon/blog/meta-xr-simulator-pc-mac-unity-unreal-developer-mixed-reality/)
- [developers.meta.com/horizon/documentation/unity/xrsim-intro](https://developers.meta.com/horizon/documentation/unity/xrsim-intro/)
- [developers.meta.com/horizon/downloads/package/meta-xr-simulator-mac-arm](https://developers.meta.com/horizon/downloads/package/meta-xr-simulator-mac-arm/)
- [developers.meta.com/horizon/documentation/unity/unity-handtracking-overview](https://developers.meta.com/horizon/documentation/unity/unity-handtracking-overview/)
- [developers.meta.com/horizon/documentation/unity/unity-spatial-anchors-overview](https://developers.meta.com/horizon/documentation/unity/unity-spatial-anchors-overview/)
- [developers.meta.com/horizon/documentation/unity/unity-passthrough-gs](https://developers.meta.com/horizon/documentation/unity/unity-passthrough-gs/)
- [developers.meta.com/horizon/documentation/unity/bb-overview](https://developers.meta.com/horizon/documentation/unity/bb-overview/)
- [developers.meta.com/horizon/documentation/unity/ps-deep-linking](https://developers.meta.com/horizon/documentation/unity/ps-deep-linking/)
- [developers.meta.com/horizon/documentation/unity/unity-upst-overview](https://developers.meta.com/horizon/documentation/unity/unity-upst-overview/)
- [developers.meta.com/horizon/blog/unity-meta-horizon-os-the-future-of-vr-unite-2025](https://developers.meta.com/horizon/blog/unity-meta-horizon-os-the-future-of-vr-unite-2025/)
- [developers.meta.com/horizon/blog/get-apps-ready-app-lab-meta-horizon-store-meta-quest-developers](https://developers.meta.com/horizon/blog/get-apps-ready-app-lab-meta-horizon-store-meta-quest-developers/)
- [developers.meta.com/horizon/resources/qfb-private-apps-dist](https://developers.meta.com/horizon/resources/qfb-private-apps-dist/)
- [meta.com/help/quest/140991407990979](https://www.meta.com/help/quest/140991407990979/) (Quest Link Windows-only confirmation)
- [github.com/Unity-Technologies/mr-example-meta-openxr](https://github.com/Unity-Technologies/mr-example-meta-openxr)
- [github.com/CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp)
- [github.com/CoderGamester/mcp-unity](https://github.com/CoderGamester/mcp-unity)
- [github.com/IvanMurzak/Unity-MCP](https://github.com/IvanMurzak/Unity-MCP)
- [github.com/AnkleBreaker-Studio/unity-mcp-server](https://github.com/AnkleBreaker-Studio/unity-mcp-server)
- [github.com/ahujasid/blender-mcp](https://github.com/ahujasid/blender-mcp)
- [github.com/github/spec-kit](https://github.com/github/spec-kit)
- [github.com/Fission-AI/OpenSpec](https://github.com/Fission-AI/OpenSpec)
- [github.com/gotalab/cc-sdd](https://github.com/gotalab/cc-sdd)
- [game.ci/docs/github/getting-started](https://game.ci/docs/github/getting-started/)
- [github.com/game-ci/unity-builder](https://github.com/game-ci/unity-builder)
- [help.managexr.com/en/articles/13199177-meta-horizon-managed-app-store-integration](https://help.managexr.com/en/articles/13199177-meta-horizon-managed-app-store-integration)

#### Blog / community (référence secondaire)
- [spin.atomicobject.com/developing-meta-quest-mac](https://spin.atomicobject.com/developing-meta-quest-mac/)
- [arborxr.com/blog/how-to-use-kiosk-mode-with-meta-quest](https://arborxr.com/blog/how-to-use-kiosk-mode-with-meta-quest)
- [vrx.vr-expert.com/arborxr-managexr-and-mhms-mdm](https://vrx.vr-expert.com/arborxr-managexr-and-mhms-mdm/)
- [thebcms.com/blog/spec-driven-development](https://thebcms.com/blog/spec-driven-development)
- [augmentcode.com/tools/best-spec-driven-development-tools](https://www.augmentcode.com/tools/best-spec-driven-development-tools)
- [communityforums.atmeta.com/discussions/dev-unity/does-meta-xr-simulator-support-mac-os/1092455](https://communityforums.atmeta.com/discussions/dev-unity/does-meta-xr-simulator-support-mac-os/1092455)
- [stackoverflow.com/questions/79322573](https://stackoverflow.com/questions/79322573/meta-quest-3-is-not-detected-in-mac-m3-silicon-chip-during-development-with-unit)

### B. Claims refutées par fact-check (à NE PAS reprendre)

| Claim refutée | Vote | Réalité |
|---|---|---|
| "OVRCameraRig prefab obligatoire pour passthrough Unity ; il faut supprimer la Main Camera" | 0-3 | Doc Meta actuelle ne l'exige pas comme universel |
| "QR codes Quest passent par 2D Android companion app development" | 0-3 | Faux — passe par Platform SDK deep linking (LaunchOtherApp + GetLaunchDetails) |
| "mr-example-meta-openxr utilise com.unity.xr.meta-openxr 0.2.1 + XRI 2.5.1 + ..." | 1-2 | Versions exactes obsolètes — résoudre via Package Manager actuel |
| "Meta XR Simulator officiellement Unity 6 sur M1 — confirmé par Meta Employee" | 1-2 | Forum post non confirmé doc officielle |
| "Mac dev fragile : Failed to load openxr runtime loader sur M2 Pro" | 1-2 | Existe mais cas isolés, pas généralité |
| "Meta Quest Link ne marche pas pour passthrough/plane Editor — Mac dev coincé" | 1-2 | Trop fort — Simulator couvre la majorité des cas |

### C. Questions ouvertes (à creuser quand pertinent)

1. **Spec-first tooling 2026** : tester en pratique GitHub Spec-Kit vs OpenSpec vs cc-sdd sur un projet pilote Ankhora avant commit
2. **Versions Unity LTS** : confirmer la combinaison stable Unity 6 LTS + Meta XR Core SDK + XR Hands + XRI + Meta-OpenXR au moment du dev (résoudre via `context7` MCP ou Package Manager)
3. **CI/CD Mac** : GameCI sur GitHub Actions Mac runners suffit-il pour builds Android Quest ? Coût/complexité réels ?
4. **Distribution Quest Store 2026** : niveau de difficulté review pour MVP enterprise, vs App Lab d'avant
5. **Recording de session XR** : tester un POC custom serialization OVRSkeleton vs explorer plugins payants (XRReplay si existe encore en 2026)
6. **Câble dédié Mac** : tester adb débit avec ton INIU + adaptateur USB-A→C avant d'investir dans un Anker USB-C ↔ USB-C
7. **Trigger QR côté apprenant** : flux exact (app Quest companion ? caméra Quest native ? scène d'accueil custom Ankhora ?) — POC dédié

### D. Stats deep-research

- **Angles décomposés** : 6
- **Sources fetchées** : 33
- **Claims extraites** : 142
- **Claims fact-checkées** : 25 (top par confidence)
- **Claims confirmées** : 19 (3-vote majority)
- **Claims refutées** : 6
- **Agents spawned** : 116
- **Total tokens** : 4.93M
- **Tool calls** : 794
- **Durée** : 14m 50s
- **Run ID** : `wf_7113415d-a0f`

---

*Ce document est versionnable. Quand un fait change (nouvelle version Unity, déprécation Meta SDK, nouveau MCP server, etc.), créer une note dans `docs/08-research/changes.md` plutôt que d'éditer ce fichier en place — il sert d'état figé de la connaissance à mai 2026.*
