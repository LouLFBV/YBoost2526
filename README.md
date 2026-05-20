# 🔫 Fyghter-Gun

**Fyghter-Gun** est un jeu de tir à la première personne (FPS) multijoueur nerveux et rapide, développé sous Unity. Affrontez vos amis dans des combats dynamiques et explosifs ! 💥

> ⚠️ **Note de développement** : Le jeu est actuellement en cours de développement et peut encore contenir plusieurs bugs ou fonctionnalités incomplètes. Vos retours et conseils sont les bienvenus pour contribuer à son amélioration !

---

## 🛠️ L'Arsenal Disponible

Utilisez un large choix d'armes adaptées à toutes les situations de combat :
* 🔥 **Fusil d’assaut** & 💣 **Fusil à pompe** pour les combats à moyenne et courte portée.
* 🎯 **Pistolet** & 👁️ **Sniper** pour la précision.
* 🔪 **Couteau** pour les éliminations au corps à corps.
* 🚀 **Lance-roquettes (RPG-7)** & 💥 **Grenades** pour des dégâts de zone massifs.
* 🌫️ **Fumigènes** pour bloquer la vision et créer des opportunités tactiques.

---

## 🚀 Architecture Logicielle & Design Patterns

Ce projet a été réalisé sur **Unity** en **C#** avec le pipeline graphique **URP (Universal Render Pipeline)**. Une attention particulière a été portée à la propreté du code et à l'évolutivité.

### 🎯 Le Pattern Strategy pour le Système d'Armes
Plutôt que d'utiliser une architecture rigide remplie de conditions (`if/else`) pour chaque type d'arme, le projet s'appuie sur le **Design Pattern Strategy**. 

Ce patron permet de modifier dynamiquement le comportement d'attaque d'un joueur en lui associant une stratégie interchangeable à la volée (Calcul par Raycast, instanciation de projectile physique, ou coup de mêlée).

* **`IWeapon` (Interface)** : Définit le contrat de base pour toutes les armes (la méthode `Attack()`).
* **`Weapon` (Classe de base)** : Contient les données partagées et l'état de l'arme (munitions, points d'impact `shootPoint`, configurations via `WeaponData`).
* **Stratégies Concrètes (`GrenadeLauncher`, `RPG7`, etc.)** : Implémentent l'interface pour définir leur propre comportement de tir (ex: instancier une grenade réseau et lui appliquer une force d'impulsion).
* **`AttackBehaviour` (Le Contexte)** : Possède une référence vers l'arme équipée. Lors du clic de tir, il appelle simplement `weaponUsed.Attack()`. Il n'a pas besoin de savoir *comment* l'arme attaque, ce qui permet d'ajouter de nouvelles armes très facilement sans modifier le reste du code.

### 🎒 Gestion de l'Inventaire (`Palette.cs`)
La sélection, l'équipement et le déséquipement sont gérés de manière découplée. La palette gère l'affichage de l'UI (icônes, munitions) et applique une logique réseau stricte :
* Masquage complet et propre des meshes (y compris des sous-éléments enfants).
* Désactivation sécurisée des scripts pour interdire de tirer avec une arme en arrière-plan.
* Gestion fluide du remplacement d'une arme de même type lors d'un ramassage au sol.

---

## 🌐 Architecture Réseau (Unity Netcode)

Le multijoueur repose sur un modèle **Serveur-Autoritaire** propulsé par **Unity Netcode for GameObjects (NGO)**, garantissant la cohérence de l'état du monde pour tous les joueurs.

### ⚡ Utilisation des RPCs (Remote Procedure Calls)
* **`[Rpc(SendTo.Server)]`** : Utilisé lorsqu'un client initie une action (ex: demander à faire apparaître une grenade). Le serveur valide la requête, attribue l'Ownership réseau au client émetteur et applique les forces physiques chez lui.
* **`[Rpc(SendTo.Everyone)]` / `NotServer`** : Permet au serveur de répercuter instantanément les effets visuels et physiques chez tous les clients (impulsions physiques, effets de particules d'explosion VFX, bruitages AudioSource).

### 📊 Détection du Suicide & Variables Réseau
* **Santé Sécurisée** : Les points de vie utilisent une `NetworkVariable<int>` modifiable uniquement par le serveur (`NetworkVariableWritePermission.Server`). Les clients s'y abonnent pour actualiser leur barre de vie locale de manière réactive.
* **Système de Score intelligent** : Lors d'une élimination par explosion (RPG ou Grenade), le serveur compare l'ID de la victime avec l'ID du lanceur (`lastAttackerId == OwnerClientId`). S'il s'agit de la même personne, c'est un suicide : le système bloque l'attribution du kill et évite les scores faussés.

---

## 📥 Installation & Utilisation

Le jeu est disponible en version jouable et téléchargeable sur la plateforme Itch.io.

### 🌐 Lien de téléchargement
Rendez-vous sur la page officielle du projet : **[Fyghter-Gun sur Itch.io](https://lawx-sama.itch.io/fyghter-gun)**

---

## 🎮 Amusez-vous bien et bon combat !
