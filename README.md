# 🌳 Tree of Life Interactive Visualization (C#)

This academic project is a **Windows Forms application** developed in **C#** to visualize the **Tree of Life** with over **36,000 nodes**. Designed for educational purposes, it targets a young audience to explore biological diversity through an intuitive and interactive experience.

## 🧠 Features

- **Radial Layout**: A circular display of the phylogenetic tree centered on "Life on Earth".
- **Smooth Navigation**: Zoom on mouse position, pan via drag-and-drop.
- **Cluster Management**: Dense subtrees are collapsed into interactive "clusters" to improve readability.
- **Interactive Info Panel**: Hover/click to reveal details (name, extinction status, taxonomy, TolWeb link).
- **High Performance**: Handles large-scale data efficiently with spatial indexing.

## 🧱 Architecture

The application follows the **MVC pattern**:

- `TreeModel.cs`, `Node.cs` → data loading & tree structure.
- `Form1.cs` → rendering and UI logic.
- `ArbreDeVieController.cs` → user input handling, layout algorithms.

## 🚀 Optimizations

- **Quadtree indexing** for fast spatial queries.
- **Dynamic radial layout** based on subtree size.
- Custom buffer panel to avoid flickering and ensure smooth rendering.


## 📂 Technologies

- C#
- Windows Forms
- .NET Framework
- Visual Studio

## 📘 License

This project was developed for academic purposes and is shared under the MIT License.
