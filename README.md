# EasySaveProject
Software developments

# EasySave Git Guidelines & Workflow

This document outlines the standard Git procedures for the EasySave project. Adherence to these practices ensures code quality, maintainability, and smooth collaboration within the ProSoft team.

---

## 1. Branch Nomenclature

We use a structured branching model to maintain a stable environment.

| Branch Category | Prefix | Example | Description |
| :--- | :--- | :--- | :--- |
| **Main** | `main` | `main` | Production-ready code and official releases (v1.0, v1.1). |
| **Development** | `develop` | `develop` | Integration branch where all features are merged. |
| **Features** | `feature/` | `feature/json-logger` | New functional developments. |
| **Fixes** | `fix/` | `fix/path-validator` | Standard bug fixes. |
| **Hotfixes** | `hotfix/` | `hotfix/critical-crash` | Urgent production fixes. |

---

## 2. Commit Message Convention

Commit messages must be concise, written in **English**, and follow the imperative mood.

**Format:** `<action>: <description>`

* **Allowed Actions:** `Add`, `Fix`, `Refactor`, `Docs`, `Test`, `Chore`.
* **Examples:**
    * `Add: implement FullBackup strategy logic`
    * `Fix: resolve null reference in FileLogger`
    * `Refactor: optimize JSON serialization loop`

---

## 3. The Development Cycle (Step-by-Step)

### Step 1: Update Local Environment
Always start from the latest version of the development branch.
```bash
git checkout develop
git pull origin develop
```

### Step 2: Create a Feature Branch
```bash
git checkout -b feature/your-feature-name
```

### Step 3: Develop and Commit
Make frequent, atomic commits. Ensure code is commented in English.
```bash
git add .
git commit -m "Add: description of the changes"
```

### Step 4: Push to Remote
```bash
git push origin feature/your-feature-name
```

### Step 5: Pull Request (PR) & Review
1.  Go to GitHub and open a **Pull Request** from `feature/your-feature-name` into `develop`.
2.  Assign a teammate for a **Code Review**.
3.  Check for: No code redundancy, English naming conventions, and .NET 8 compatibility.

### Step 6: Finalize
Once the PR is merged, clean up your local branches.
```bash
git checkout develop
git pull origin develop
git branch -d feature/your-feature-name
```

---

## 4. ProSoft Quality Checklist
- [ ] Code compiles in Visual Studio 2022.
- [ ] Language: **English** (Code, Comments, Docs).
- [ ] Architecture: Design patterns (Factory, Strategy, etc.) respected.
- [ ] Performance: Minimal duplicate lines of code.
```
