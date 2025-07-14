# Interactive Tree of Life Visualization in C#

![Language](https://img.shields.io/badge/language-C%23-9B4F96?style=flat-square)
![Platform](https://img.shields.io/badge/platform-Windows-0078D6?style=flat-square)
![Framework](https://img.shields.io/badge/framework-WinForms-blue?style=flat-square)
![IDE](https://img.shields.io/badge/IDE-Visual%20Studio-5C2D91?style=flat-square)

A high-performance desktop application for visualizing the Tree of Life, developed in C# with Windows Forms. This academic project demonstrates how to handle and render large hierarchical datasets (over 36,000 nodes) efficiently by implementing advanced data structures and custom layout algorithms.

## Table of Contents

- [About The Project](#about-the-project)
- [Key Features](#key-features)
- [Built With](#built-with)
- [Getting Started](#getting-started)
- [Technical Deep Dive](#technical-deep-dive)
  - [Performance Optimization: The Quadtree](#performance-optimization-the-quadtree)
  - [Data Readability: Custom Radial Layout](#data-readability-custom-radial-layout)
- [Screenshots](#screenshots)
- [Future Improvements](#future-improvements)
- [License](#license)

## About The Project

This project is a data visualization tool designed to explore the phylogenetic Tree of Life in an interactive and educational way. The core challenge was to create a smooth and responsive user experience despite the massive dataset, making complex biological data accessible and engaging.

## Key Features

-   **Large-Scale Data Handling**: Efficiently loads, processes, and renders over 36,000 nodes.
-   **Interactive Radial Layout**: Displays the tree in an intuitive circular format.
-   **Smooth Navigation**: Features seamless zooming (centered on the mouse) and panning (drag-and-drop).
-   **Dynamic Clustering**: Automatically groups dense node clusters to maintain readability at any zoom level.
-   **Detailed Info Panel**: Shows specific information for each node on hover.

## Built With

-   **C#**
-   **.NET Framework** (with Windows Forms & GDI+)
-   **Visual Studio**

## Getting Started

To run this project, you will need Visual Studio with the .NET desktop development workload installed.

1.  **Clone the repository:**
    ```sh
    git clone https://github.com/nico916/tree-of-life-csharp.git
    ```
2.  **Open the solution:**
    Navigate to the project folder and open the `.sln` file with Visual Studio.
3.  **Run the application:**
    Press `F5` or click the "Start" button in Visual Studio to compile and run the project.

## Technical Deep Dive

The application is built on an **MVC (Model-View-Controller)** architecture to separate data logic from the user interface. The most significant technical challenges were performance and data readability.

### Performance Optimization: The Quadtree

-   **Problem**: With 36,000+ nodes, detecting which node is under the mouse cursor via a simple loop would be incredibly slow (O(N) complexity), causing the UI to freeze.
-   **Solution**: I implemented a **Quadtree**, a spatial partitioning data structure. It recursively divides the 2D space into four quadrants, allowing for extremely fast spatial queries. This reduces the search complexity to O(log N), ensuring that interactions like hovering and clicking remain instantaneous, regardless of the dataset size.

### Data Readability: Custom Radial Layout

-   **Problem**: A standard radial layout would allocate equal angular space to each main branch, causing dense branches (like insects) to become an unreadable, overlapping mess.
-   **Solution**: I designed a custom layout algorithm that allocates angular space **proportionally to the number of descendants** in each subtree. This gives larger branches more room to expand, creating a much more balanced and legible visualization.

## Future Improvements

-   Implement a search function to find species by name.
-   Improve label rendering in highly dense areas to prevent any overlap.
-   Integrate more interactive data, such as images and detailed descriptions for selected species.

## License

Distributed under the MIT License. See `LICENSE` file for more information.
