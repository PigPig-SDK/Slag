<h1>
  <img src="images/slag.png" alt="icon" width="40"/>
  Slag
</h1>
Which stands for Simple Luminiferous Associated Geometrification, an acronym I made up just to waste your time.
<br>
<br>
Built for learning purposes, not as a competitor to existing modeling tools. This project explores OpenGL rendering within [Avalonia UI](https://avaloniaui.net/), which turned out to be a surprisingly pleasant combination.

## Showcase

<div align="center">
  <video src="https://github.com/user-attachments/assets/4f5b794a-1bff-48a5-9b1f-02323d48b5cf" height="256" autoplay loop muted playsinline></video>
  <video src="https://github.com/user-attachments/assets/d38d3ef3-e811-4424-a8c2-64a2ebbd6145" height="256" autoplay loop muted playsinline></video>
</div>

## Features
- **Multiple Selection Modes** — Mesh, Vertex, Edge, and Face
- **Mesh Modifiers** — Extrude, Merge, Delete, Move, Scale, Rotate and Flip
- **Object Transformations** — Per-object Scale, Rotation, and Translation
- **Vertex Snapping** for precision modeling
- **Shadow Mapping** for accurate depth visualization
- **OBJ Import/Export** for broad compatibility
- **Undo/Redo** support
- **Minimalist UI** designed to stay out of your way

## Limitations
- **Occasional crashes** — the app is not yet stable enough for serious use. I mean its a toy project anyhow.
- Only supports .OBJ for simplicity. Saving scenes also stores SLAG specific metadata in the .OBJ file.
- Preformance degrades with extremely high poly-count meshes. (Upward of 500,000 triangles)
- Can be clunky if you're unfamiliar with the workflow.
- Displaying face selection and edge selection can sometimes be confusing.

## Conclusion
This project taught me a lot about OpenGL, making a solid entity component system, separating concerns and writing unsafe code in C#. The OpenGL workflow in this project is relatively painless. The only annoyance was tracking down driver functions 
that Avalonia doesn't expose, made worse by the older version of OpenGL it provides.


If I were to approach a project like this again, I'd focus on stability and better utilizing abstraction. Currently the mesh's structure is specifically tuned for buffer into OpenGL quickly. While this is great for producing results, it makes writing mesh modifiers undesirably complex.


If I could have my way, everything would be thoroughly tested, and perfect - but life is finite, and so mistakes persist.

-Sean
