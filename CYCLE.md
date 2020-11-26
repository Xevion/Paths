# Development Cycle

A small blog-like detail of this project as it was developed over time. Streamable links,   

- [2020-11-09_20-19-03.gif](https://streamable.com/cttb84)
    - First known record of the project as a screenshot. Shows basic A* pathfinding with my Grid Shader, but there aren't even any animations, timings or controls of any sort yet.
    However, the colors haven't changed.
- [2020-11-09_21-07-52.gif](https://streamable.com/scr4o1)
    - Animations were added.
- [2020-11-09_21-21-34.mp4](https://streamable.com/bzjg9g)
    - I started recording video instead of GIF due to file sizes limit, but this added whatever music I was playing.

    *Lil West - bit my tongue now my mouth is bleeding (osno1 remix)*
- [2020-11-09_21-33-49.mp4](https://streamable.com/zl61zg)
    - Around this time, I still had a bug which caused a white border of cells to always generate, or always be rendered. To this day, I believe it may have been a combination of both?
    Regardless, the issue was fixed, but many videos were recorded with this issue still present.

    *SEBii - igotitALL*
- [2020-11-09_21-35-38.mp4](https://streamable.com/aw1gu9)
    - I believe this is showing off speed changes. It's probably about the time at which I added state increment clamping (essentially, the speed would be clamped to a range which made sure the animation played for at least a certain duration), smarter TPS calculations (using Unity's Time.deltaTime) and so on.

    *SEBii - igotitALL*
- [2020-11-09_23-22-54.mp4](https://streamable.com/3lck7i)

    *Lil West - bit my tongue now my mouth is bleeding (osno1 remix)*
- [2020-11-09_23-27-42.mp4](https://streamable.com/lzejk5)

    *Lil West - bit my tongue now my mouth is bleeding (osno1 remix)*
- [2020-11-13_19-37-33.mp4](https://streamable.com/u5mn0g)
    - After a little break from programming, I came back and started increasing the size of the grid.
    The grid's size was still a square at the moment, and it was completely unoptimized, so I began looking into it.
    One of the first optimizations was turning down the number of states, essentially, all Seen nodes were added at once, instead of one by one (like it is today).

    *BadMoodRude - Miss You ft. 6obby*
- [Unity_2020-11-13_20-43-44.png](https://i.imgur.com/S7x7oDl.png)
    - I began modifying the grid to support more than a square, but I had to start working with ratios. Without it, I'd get really really weird looking grids, like you see here.
    This grid looks strange because the height is too large, but the ratio is 1:1 (it needs to be 1:3.5 or something, probably).
- [Unity_2020-11-13_21-21-30.png](https://i.imgur.com/AnGfXIp.png)
    - Camera/Grid calculations completed. Now, the camera's screen size and grid size are calculated on startup and will fit just about any dimensions I can try.
- [2020-11-13_21-31-46.mp4](https://streamable.com/di62yf)

    *Jadeci - Mobile Suit Fang*
- [2020-11-13_21-33-30.mp4](https://streamable.com/bxvmml)
    - You can still see the amount of time taken up by just calculating the path. It's awful!
- [2020-11-14_00-41-34.mp4](https://streamable.com/a2ihay)
    - Increasing the speed a ton to the point where it's *only* pathfinding and displaying a single frame basically, and then having fun with music that fast.

    *Younger Brother - Pound a Rhythm Electronic*
- [2020-11-14_00-42-58.mp4](https://streamable.com/e5yigq)
    - Same thing as before.
- [2020-11-14_15-31-29.mp4](https://streamable.com/fi2nld)
    - Around this time, I began playing with new grid generation methods. I tried random, I tried drunkard walk, I tried rendering big rectangles... I didn't really like what occurred.
    But still, it's part of the development cycle, and you can see what turned out here.

    *JVLA - Such a Whore (Stellular Remix)*
- [2020-11-24_08-46-12.mp4](https://streamable.com/ldbfyh)
    - There's nothing obvious here that changed, but this is when I began profiling and optimizing a ton. I had suspicions around the time that something in the algorithm was acting weird, but I couldn't figure it out.

    *6 dogs - someone (angelwinter flip)*
- [Unity_2020-11-24_17-25-07.png](https://i.imgur.com/nC9mlKf.png)
    - Performance gains! The only interesting thing here is the timing info. The algorithm has trawled through 95% of the graph and yet only 57ms were took doing it! Victory.
- [Unity_2020-11-24_18-31-41.png](https://i.imgur.com/aQmDS5z.png)
    - An hour later, even better performance. 20ms for the entire graph. No idea what I did specifically in between though. I remember removing costly `List` methods and then implementing the `Change` and `ChangeController` struct/classes, so this second image must be from the `Change` implementation (which replaced `GridState` and `RecordState()`).
- [2020-11-24_18-47-43.mp4](https://streamable.com/q656da)
    - Showing off animations of the HUGE performance gains. 2-10ms pathfinding usually!

    *Armand Sauvage - Nimbus*
- [2020-11-24_22-05-40.mp4](https://streamable.com/l186tk)
    - I don't remember what I recorded this for specifically, but I do still notice how the path changes sit on top of the End node here.
- [2020-11-25_02-26-18.mp4](https://streamable.com/8rmoeh)
    - Not pathfinding related, showing off that I got the very annoying vector math working. Must've been 50-60 lines of code all so I could just render those blue squares and that blue text.
    More specifically, it was getting the position of the mouth, turning that into a world position, turning that world position into a grid position (int normalization), adding text,  then turning it back into a world position and rendering the blue square. More complex than it sounds, less annoying than I make it sound. Was just a lot of work for a very simple feature.

    *AES DNA - Inks*
- [2020-11-25_10-02-35.mp4](https://streamable.com/u1exn3)
    - Successful grid editing test. This was actually pretty cool for me because usually things don't work on the first try. I implemented the entire UIController editing tools (with start/end node movement) on the first try.
    Of course, later tests would reveal quirks that needed amendment as I implemented animation states and the slider, but editing worked very well here.

    *Gucci Mane Ft. Drake - Both (SHARPS x TYNVN Remix)*
- [2020-11-25_11-11-16.mp4](https://streamable.com/o0iavu)
    - Shows off complete animations in conjunction with reloading. Paths are calculated immediately, and the second the grid is edited, they are recalculated and shown again. Pretty huge step, and required a lot of code to work out the logic. Controls like space were added here, and the slider was added some time before (although it was not functional at the time of recording, see next clip).

    *YAMEII - NEONDREAMS*
- [2020-11-25_12-50-19.mp4](https://streamable.com/3wsxn0)
    - A larger grid, a better demo showing how it does pathfinding. Also, the slider is now functional and included in the animation. It isn't properly demonstrated though, I forgot to show that.

    *chernoburkv - temperatura*
- [2020-11-25_12-52-47.mp4](https://streamable.com/1p1hpy)
    - A second demo of the previous video, now showing how the slider can be moved to control the state of the animation. Functionality here slightly differs from what it is now.
