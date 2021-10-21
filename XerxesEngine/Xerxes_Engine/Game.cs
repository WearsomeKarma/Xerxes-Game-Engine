﻿using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.IO;
using Xerxes_Engine.Systems.Graphics;
using OpenTK.Graphics;
using Xerxes_Engine.Systems.Input;
using Math_Helper = Xerxes_Engine.Tools.Math_Helper;
using Xerxes_Engine.Systems.Serialization;
using Xerxes_Engine.Systems.Graphics.R2;
using Xerxes_Engine.Engine_Objects;
using Xerxes_Engine.Systems.OpenTK_Input;
using OpenTK.Input;

namespace Xerxes_Engine
{
    /// <summary>
    /// Contains a Game Window object, and hooks events to it.
    /// It downstreams these events using the generic Streamline_Arguments
    /// and downstreams. It takes a SA__Draw upstream.
    /// This object is parentless - Xerxes_Childless
    /// </summary>
    public class Game : 
        Xerxes_Object<Game>, 
        IXerxes_Ancestor_Of<Scene> 
    {
        internal GameWindow Game__GAME_WINDOW__Internal { get; }

        //PATHS
        public string Game__DIRECTORY__BASE { get; }
        public string Game__DIRECTORY__ASSETS { get; }
        public string Game__DIRECTORY__SHADERS { get; }
        
        #region Systems

        private List<Game_System> Game__SYSTEMS { get; }
        
        /// <summary>
        /// Responsible for loading and unloading textures.
        /// </summary>
        public Asset_Pipe Game__Asset_Provider { get; private set; }
        /// <summary>
        /// Responsible for recording and recieving loaded sprites.
        /// </summary>
        public Vertex_Object_Library Game__Vertex_Object_Library { get; private set; }
        public Sprite_Library Game__Sprite_Library { get; private set; }
        internal Render_Service Game__Render_Service { get; private set; }
        //public Text_Displayer Game__Text_Displayer { get; private set; }
        public Input_System Game__Input_System { get; private set; }
        //public Sprite_Animation_Library Game__Animation_Schematic_Library { get; private set; }
        //public Scene_Manager Game__Scene_Management_Service { get; private set; }
        #endregion

        #region Time
        private Timer _Game__UPDATE_TIMER { get; }
        private Timer _Game__RENDER_TIMER { get; }

        public double Game__Elapsed_Time__Update => _Game__UPDATE_TIMER.Timer__Time_Elapsed;
        public double Game__Delta_Time__Update   => _Game__UPDATE_TIMER.Timer__Delta_Time;
        public double Game__Elapsed_Time__Render => _Game__RENDER_TIMER.Timer__Time_Elapsed;
        public double Game__Delta_Time__Render   => _Game__RENDER_TIMER.Timer__Delta_Time;
        #endregion

        public int Game__Window_Width => Game__GAME_WINDOW__Internal.Width;
        public int Game__Window_Height => Game__GAME_WINDOW__Internal.Height;
        public Vector2 Get__Window_Size__Game()
            => new Vector2(Game__Window_Width, Game__Window_Height);

        public float Get__Window_Hypotenuse__Game()
            => Math_Helper.Get__Hypotenuse(Game__Window_Width, Game__Window_Height);
        
        public Game()
            : this (Game_Arguments.Get__Default__Game_Arguments())
        {}

        public Game(Game_Arguments game_Argument)
        {
            _Game__UPDATE_TIMER = new Timer(-1);
            _Game__RENDER_TIMER = new Timer(-1);

            Protected_Declare__Downstream_Source__Xerxes_Engine_Object
                <SA__Associate_Game>();
            Protected_Declare__Downstream_Source__Xerxes_Engine_Object
                <SA__Update>();
            Protected_Declare__Downstream_Source__Xerxes_Engine_Object
                <SA__Render>();
            Protected_Declare__Downstream_Source__Xerxes_Engine_Object
                <SA__Resize_2D>();

            Protected_Declare__Downstream_Source__Xerxes_Engine_Object
                <SA__Input_Mouse_Button>();
            Protected_Declare__Downstream_Source__Xerxes_Engine_Object
                <SA__Input_Mouse_Move>();

            Protected_Declare__Upstream_Catch__Xerxes_Engine_Object
                <SA__Draw>
                (
                    Private_Handle__Draw__Game
                );

            Game__GAME_WINDOW__Internal = 
                new GameWindow
                (
                    (int)
                    (
                        game_Argument?.Game_Arguments__WINDOW_WIDTH 
                        ?? Game_Arguments.Game_Arguments__DEFAULT_WINDOW_WIDTH
                    ),
                    (int)
                    (
                        game_Argument?.Game_Arguments__WINDOW_HEIGHT
                        ?? Game_Arguments.Game_Arguments__DEFAULT_WINDOW_HEIGHT
                    ),

                    GraphicsMode.Default, 
                    
                    game_Argument?.Game_Arguments__WINDOW_TITLE
                    ?? Game_Arguments.Game_Arguments__DEFAULT_WINDOW_TITLE
                );

            Game__SYSTEMS = new List<Game_System>();
            
            Log.Initalize__Log
            (
                game_Argument
            );

            Game__DIRECTORY__BASE = AppDomain.CurrentDomain.BaseDirectory; 
            Game__DIRECTORY__ASSETS = 
                Private_Validate__Directory__Game
                (
                    game_Argument.Game_Arguments__ASSET_DIRECTORY,
                    Game_Arguments.Game_Arguments__DEFAULT_ASSET_DIRECTORY
                );
            Game__DIRECTORY__SHADERS = 
                Private_Validate__Directory__Game
                (
                    game_Argument.Game_Arguments__SHADER_DIRECTORY,
                    Game_Arguments.Game_Arguments__DEFAULT_SHADER_DIRECTORY
                );
            
            Private_Establish__Base_Systems__Game();
            Private_Establish__Custom_Systems__Game();
            Private_Load__Systems__Game();
            
            Game__Render_Service.Internal_Load__Shaders__Render_Service(Get__Shaders__Game());

            Private_Hook__To_Game_Window__Game();

            //END SERVICES

            Log.Internal_Write__Verbose__Log(Log.VERBOSE__GAME__CONTENT_LOADING, this);
            Handle_Load__Content__Game();
            Log.Internal_Write__Verbose__Log(Log.VERBOSE__GAME__CONTENT_LOADED, this);
        }

        internal override void Internal_Handle__Sealed__Xerxes_Engine_Object()
        {
            bool success =
                Protected_Invoke__Descending_Streamline__Xerxes_Engine_Object
                <SA__Associate_Game>(new SA__Associate_Game(-1,-1, this));

            Log.Internal_Write__Verbose__Log("game seal success: {0}", this, success);
        }

        public void Run__Game()
        {
            Log.Internal_Write__Info__Log
            (
                Log.INFO__GAME__RUN_INVOKED,
                this
            );

            Internal_Seal__Xerxes_Engine_Object();

            Game__GAME_WINDOW__Internal.Run();
        }

        private void Private_Hook__To_Game_Window__Game()
        {
            Game__GAME_WINDOW__Internal.Resize += Private_Handle__Resize_Window__Game;
            Game__GAME_WINDOW__Internal.Closed += Private_Handle__Closed_Window__Game;

            Game__GAME_WINDOW__Internal.UpdateFrame += Private_Handle__Update_Window__Game;
            Game__GAME_WINDOW__Internal.RenderFrame += Private_Handle__Render_Window__Game;

            Game__GAME_WINDOW__Internal.Load += Private_Handle__Load_Window__Game;
            Game__GAME_WINDOW__Internal.Unload += Private_Handle__Unload_Window__Game;

            Game__GAME_WINDOW__Internal.MouseDown += Private_Handle__Mouse_Down__Game;
            Game__GAME_WINDOW__Internal.MouseUp   += Private_Handle__Mouse_Up__Game;
            Game__GAME_WINDOW__Internal.MouseMove += Private_Handle__Mouse_Move__Game;
        }

        private string Private_Validate__Directory__Game
        (
            string baseDirectory,
            string recoveryDirectory
        )
        {
            if (baseDirectory == null || !Directory.Exists(baseDirectory))
            {
                Log.Internal_Write__Log
                (
                    Log_Message_Type.Error__IO,
                    Log.ERROR__GAME__DIRECTORY_NOT_FOUND_1,
                    this,
                    baseDirectory
                );

                Log.Internal_Write__Warning__Log(Log.WARNING__RECOVERY__ASSET_DIRECTORY_NOT_FOUND, this);


                baseDirectory = Private_Get__Validated_Default_Directory__Game(recoveryDirectory);

                if (baseDirectory != null)
                    return baseDirectory;

                Log.Internal_Panic__Log(Log.ERROR__GAME__RECOVERY_DIRECTORY_NOT_FOUND_1, this, recoveryDirectory);

                Game__GAME_WINDOW__Internal.Close();
            }

            return baseDirectory;
        }

        private string Private_Get__Validated_Default_Directory__Game(string defaultDirectory)
        {
            if (!Directory.Exists(defaultDirectory))
            {
                Log.Internal_Write__Log
                (
                    Log_Message_Type.Error__IO,
                    Log.ERROR__GAME__RECOVERY_DIRECTORY_NOT_FOUND_1,
                    this,
                    defaultDirectory
                );

                return null;
            }

            return defaultDirectory;
        }

        private void Private_Handle__Resize_Window__Game(object sender, EventArgs e)
        {
            GL.Viewport(Game__GAME_WINDOW__Internal.ClientRectangle);
            Game__Render_Service
                .Establish__Orthographic_Projection__Render_Service
                (
                    Game__GAME_WINDOW__Internal.Width, 
                    Game__GAME_WINDOW__Internal.Height
                );
            SA__Resize_2D resize_2D_Argument = 
                new SA__Resize_2D
                (
                    Game__Elapsed_Time__Update,
                    Game__Delta_Time__Update,
                    Game__Window_Width,
                    Game__Window_Height
                );

            Protected_Invoke__Descending_Streamline__Xerxes_Engine_Object
                <SA__Resize_2D>(resize_2D_Argument);
        }

        private void Private_Handle__Closed_Window__Game(object sender, EventArgs e)
        {
            
        }

        private void Private_Handle__Update_Window__Game(object sender, FrameEventArgs e)
        {
            _Game__UPDATE_TIMER.Progress__Timer(e.Time);
            SA__Update frame_Argument = 
                new SA__Update
                (
                    Game__Elapsed_Time__Update, 
                    e.Time
                );

            Protected_Invoke__Descending_Streamline__Xerxes_Engine_Object
                <SA__Update>(frame_Argument);
        }

        private void Private_Handle__Render_Window__Game(object sender, FrameEventArgs e)
        {
            _Game__RENDER_TIMER.Progress__Timer(e.Time);
            SA__Render frame_Argument = 
                new SA__Render
                (
                    Game__Elapsed_Time__Render, 
                    e.Time
                );

            Game__Render_Service.Internal_Begin__Render_Service();

            Protected_Invoke__Descending_Streamline__Xerxes_Engine_Object
                <SA__Render>(frame_Argument);

            Game__Render_Service.Internal_End__Render_Service();
            Game__GAME_WINDOW__Internal.SwapBuffers();
        }

        private void Private_Handle__Draw__Game(SA__Draw e)
        {
            Game__Render_Service
                .Draw__Render_Service
                (
                    e
                );
        }

        private void Private_Handle__Load_Window__Game(object sender, EventArgs e)
        {
            Protected_Handle__Load__Game();
        }

        protected virtual void Protected_Handle__Load__Game()
        {

        }

        private void Private_Handle__Unload_Window__Game(object sender, EventArgs e)
        {
            Private_Unload__Systems__Game();
            Protected_Handle__Unload__Game();
        }

        protected virtual void Protected_Handle__Unload__Game()
        {

        }

        private void Private_Handle__Mouse_Move__Game
        (
            object sender,
            MouseMoveEventArgs e
        )
        {
            SA__Input_Mouse_Move input_Mouse_Move = 
                new SA__Input_Mouse_Move
                (
                    _Game__UPDATE_TIMER.Timer__Time_Elapsed,
                    _Game__UPDATE_TIMER.Timer__Delta_Time,
                    e
                );

            Protected_Invoke__Descending_Streamline__Xerxes_Engine_Object
            (input_Mouse_Move);
        }

        private void Private_Handle__Mouse_Down__Game
        (
            object sender,
            MouseButtonEventArgs e
        )
        {
            SA__Input_Mouse_Button input_Mouse_Button =
                new SA__Input_Mouse_Button
                (
                    _Game__UPDATE_TIMER.Timer__Time_Elapsed,
                    _Game__UPDATE_TIMER.Timer__Delta_Time,
                    e
                );

            Protected_Invoke__Descending_Streamline__Xerxes_Engine_Object
            (input_Mouse_Button);
        }

        private void Private_Handle__Mouse_Up__Game
        (
            object sender,
            MouseButtonEventArgs e
        )
        {
            SA__Input_Mouse_Button input_Mouse_Button =
                new SA__Input_Mouse_Button
                (
                    _Game__UPDATE_TIMER.Timer__Time_Elapsed,
                    _Game__UPDATE_TIMER.Timer__Delta_Time,
                    e
                );

            Protected_Invoke__Descending_Streamline__Xerxes_Engine_Object
            (input_Mouse_Button);
        }

        private void Private_Unload__Systems__Game()
        {
            Log.Internal_Write__Verbose__Log(Log.VERBOSE__GAME__SYSTEMS__UNLOADING, this);

            foreach (Game_System gamesys in Game__SYSTEMS)
            {   
                gamesys.Internal_Unload__Game_System();
                Log.Internal_Write__Verbose__Log(Log.VERBOSE__GAME__SYSTEM__UNLOADED_1, this, gamesys);
            }

            Log.Internal_Write__Verbose__Log(Log.VERBOSE__GAME__SYSTEMS__UNLOADED, this);
        }

        public T Get_System__Game<T>() where T : Game_System
        {
            foreach (Game_System system in Game__SYSTEMS)
                if (system is T && system.Accessable)
                    return system as T;

            Log.Internal_Write__Log
            (
                Log_Message_Type.Error__System, 
                Log.ERROR__SYSTEM__NOT_FOUND_1, 
                this, 0, typeof(T).ToString()
            );
            return null;
        }

        protected bool Register__System__Game<T>(T gameService) where T : Game_System
        {
            if (Game__SYSTEMS.Exists((s) => s is T))
            {
                Log.Internal_Write__Warning__Log
                (
                    Log.WARNING__GAME__SYSTEM__ALREADY_LOADED_1,
                    this,
                    0,
                    gameService?.ToString()
                );
                return false;
            }
            
            Log.Internal_Write__Verbose__Log(Log.VERBOSE__GAME__SYSTEM__LOADED_1, this, gameService?.ToString());
            
            Game__SYSTEMS.Add(gameService);

            return true;
        }

        protected virtual string[] Get__Shaders__Game()
        {
            return new string[] { "shader" };
        }

        private void Private_Establish__Base_Systems__Game()
        {
            Private_Initalize__Base_Systems__Game();
            Private_Register__Base_Systems__Game();
        }

        private void Private_Initalize__Base_Systems__Game()
        {
            Log.Internal_Write__Verbose__Log(Log.VERBOSE__GAME__SYSTEMS__INITALIZING, this);
            
            Game__Asset_Provider = new Asset_Pipe(this);
            Game__Vertex_Object_Library = new Vertex_Object_Library(this);
            Game__Sprite_Library = new Sprite_Library(this);
            Game__Render_Service = new Render_Service(this, Game__GAME_WINDOW__Internal.Width, Game__GAME_WINDOW__Internal.Height);
            //Game__Text_Displayer = new Text_Displayer(this);
            Game__Input_System = new Input_System(this);
            //Game__Animation_Schematic_Library = new Sprite_Animation_Library(this);
            //Game__Scene_Management_Service = new Scene_Manager(this);

            Log.Internal_Write__Verbose__Log(Log.VERBOSE__GAME__SYSTEMS__INITALIZED, this);
        }

        private void Private_Register__Base_Systems__Game()
        {
            Log.Internal_Write__Verbose__Log(Log.VERBOSE__GAME__SYSTEMS__REGISTERING, this);
            
            Register__System__Game(Game__Asset_Provider);
            Register__System__Game(Game__Vertex_Object_Library);
            Register__System__Game(Game__Sprite_Library);
            Register__System__Game(Game__Render_Service);
            //Register__System__Game(Game__Text_Displayer);
            Register__System__Game(Game__Input_System);
            //Register__System__Game(Game__Animation_Schematic_Library);
            //Register__System__Game(Game__Scene_Management_Service);

            Log.Internal_Write__Verbose__Log(Log.VERBOSE__GAME__SYSTEMS__REGISTERED, this);
        }

        private void Private_Establish__Custom_Systems__Game()
        {
            Log.Internal_Write__Verbose__Log(Log.VERBOSE__GAME__SYSTEMS_CUSTOM__REGISTERING, this);

            Handle_Register__Custom_Systems__Game();
    
            Log.Internal_Write__Verbose__Log(Log.VERBOSE__GAME__SYSTEMS_CUSTOM__REGISTERED, this);
        }

        private void Private_Load__Systems__Game()
        {
            Log.Internal_Write__Verbose__Log(Log.VERBOSE__GAME__SYSTEMS__LOADING, this);

            foreach (Game_System system in Game__SYSTEMS)
                system.Internal_Load__Game_System();

            Log.Internal_Write__Verbose__Log(Log.VERBOSE__GAME__ALL_SYSTEMS__LOADED, this);
        }

        protected virtual void Handle_Register__Custom_Systems__Game() { }
        protected virtual void Handle_Load__Content__Game() { }
    }
}
