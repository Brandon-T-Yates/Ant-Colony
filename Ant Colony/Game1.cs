using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace AntSimulator
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Texture2D _workerAntTexture;
        private Texture2D _queenAntTexture;
        private Texture2D _foodTexture;
        private Texture2D _backgroundTexture; // New variable for background image

        private List<Ant> _ants;
        private List<Food> _foods;
        private TimeSpan _workerAntSpawnTimer;
        private TimeSpan _foodSpawnTimer;

        private const int MaxWorkerAnts = 100000;
        private const double WorkerAntSpawnInterval = 30; // in seconds
        private const int FoodSpawnInterval = 10; // in seconds

        private const int WorkerAntSpeed = 2;
        private const int QueenAntSpeed = 1;

        private int _screenWidth;
        private int _screenHeight;

        private SpriteFont _font;


        private const int AntSize = 32 / 8; // 1/8th the size
        private const int QueenSize = 40 / 8; // 1/8th the size
        private const int FoodSize = 16 / 8; // 1/8th the size

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _ants = new List<Ant>();
            _foods = new List<Food>();

            _workerAntSpawnTimer = TimeSpan.FromSeconds(WorkerAntSpawnInterval);
            _foodSpawnTimer = TimeSpan.FromSeconds(FoodSpawnInterval);

            _screenWidth = GraphicsDevice.Viewport.Width;
            _screenHeight = GraphicsDevice.Viewport.Height;

            // Spawn 1 queen ant
            SpawnQueenAnt();

            // Spawn 3 worker ants
            for (int i = 0; i < 3; i++)
            {
                SpawnWorkerAnt();
            }

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _workerAntTexture = Content.Load<Texture2D>("worker_ant");
            _queenAntTexture = Content.Load<Texture2D>("queen_ant");
            _foodTexture = Content.Load<Texture2D>("food");
            _backgroundTexture = Content.Load<Texture2D>("background"); // Load the background image
            _font = Content.Load<SpriteFont>("Font");

        }

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            _workerAntSpawnTimer -= gameTime.ElapsedGameTime;
            if (_workerAntSpawnTimer <= TimeSpan.Zero && _ants.Count < MaxWorkerAnts)
            {
                SpawnWorkerAnt();
                _workerAntSpawnTimer = TimeSpan.FromSeconds(WorkerAntSpawnInterval);
            }

            _foodSpawnTimer -= gameTime.ElapsedGameTime;
            if (_foodSpawnTimer <= TimeSpan.Zero)
            {
                SpawnFood();
                _foodSpawnTimer = TimeSpan.FromSeconds(FoodSpawnInterval);
            }

            foreach (var ant in _ants)
            {
                ant.Update(gameTime, _screenWidth, _screenHeight);

                if (ant.IsCarryingFood)
                {
                    if (Vector2.Distance(ant.Position, ant.HomePosition) < 2)
                    {
                        ant.DropFood();
                    }
                    else
                    {
                        ant.MoveTowards(ant.HomePosition);
                    }
                }
                else
                {
                    foreach (var food in _foods)
                    {
                        if (!food.IsPickedUp && Vector2.Distance(ant.Position, food.Position) < 50)
                        {
                            ant.MoveTowards(food.Position);
                            if (Vector2.Distance(ant.Position, food.Position) < 2)
                            {
                                ant.PickUpFood(food);
                                break;
                            }
                        }
                    }
                }
            }

            base.Update(gameTime);
        }


        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black); // Clear the screen with black color

            _spriteBatch.Begin();

            // Draw the background image
            _spriteBatch.Draw(_backgroundTexture, new Rectangle(0, 0, _screenWidth, _screenHeight), Color.White);

            foreach (var ant in _ants)
            {
                Vector2 origin;
                float rotation;

                if (ant.IsQueen)
                {
                    origin = new Vector2(_queenAntTexture.Width / 2, _queenAntTexture.Height / 2);
                    rotation = 0f;
                }
                else
                {
                    origin = new Vector2(_workerAntTexture.Width / 2, _workerAntTexture.Height / 2);
                    rotation = ant.Rotation;
                }

                SpriteEffects effects = ant.IsQueen ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

                Texture2D antTexture = ant.IsQueen ? _queenAntTexture : _workerAntTexture;

                _spriteBatch.Draw(antTexture, ant.Position, null, Color.White, rotation, origin, 1f / 8f, effects, 0);
            }

            foreach (var food in _foods)
            {
                if (!food.IsPickedUp)
                {
                    _spriteBatch.Draw(_foodTexture, food.Position, null, Color.White, 0f, Vector2.Zero, 1f / 8f, SpriteEffects.None, 0);
                }
            }

            string totalAntCount = "Total Ants: " + _ants.Count.ToString();
            Vector2 totalAntCountPosition = new Vector2(10, 10);
            _spriteBatch.DrawString(_font, totalAntCount, totalAntCountPosition, Color.White);

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void SpawnWorkerAnt()
        {
            Random random = new Random();
            Vector2 position = new Vector2(random.Next(_screenWidth - AntSize), random.Next(_screenHeight - AntSize));

            _ants.Add(new Ant(position, WorkerAntSpeed, AntSize));
        }

        private void SpawnQueenAnt()
        {
            Random random = new Random();
            Vector2 position = new Vector2(random.Next(_screenWidth - QueenSize), random.Next(_screenHeight - QueenSize));

            _ants.Add(new Ant(position, QueenAntSpeed, QueenSize, true));
        }

        private void SpawnFood()
        {
            Random random = new Random();
            Vector2 position = new Vector2(random.Next(_screenWidth - FoodSize), random.Next(_screenHeight - FoodSize));

            _foods.Add(new Food(position, FoodSize));
        }
    }

    public class Ant
    {
        private Vector2 _position;
        private Vector2 _velocity;
        private readonly int _speed;
        private readonly int _size;
        private bool _isCarryingFood;
        private Vector2 _homePosition;
        private float _rotation;
        public float Rotation { get { return _rotation; } }

        public Vector2 Position { get { return _position; } }
        public bool IsQueen { get; }
        public bool IsCarryingFood { get { return _isCarryingFood; } }
        public Vector2 HomePosition { get { return _homePosition; } }

        public Ant(Vector2 position, int speed, int size, bool isQueen = false)
        {
            _position = position;
            _velocity = Vector2.Zero;
            _speed = speed;
            _size = size;
            _isCarryingFood = false;
            _homePosition = position;
            IsQueen = isQueen;
        }

        public void MoveTowards(Vector2 targetPosition)
        {
            Vector2 direction = Vector2.Normalize(targetPosition - _position);
            _position += direction * _speed;
        }

        public void Update(GameTime gameTime, int screenWidth, int screenHeight)
        {
            if (IsQueen)
            {
                // Queen ant logic
                // Example: Queen moves slowly in a circular pattern
                float angle = (float)(gameTime.TotalGameTime.TotalSeconds * Math.PI * 0.25);
                float radius = 50;
                float x = (float)Math.Cos(angle) * radius;
                float y = (float)Math.Sin(angle) * radius;
                _position = new Vector2(screenWidth / 2 + x, screenHeight / 2 + y);
            }
            else
            {
                // Worker ant logic
                // Example: Move randomly
                Random random = new Random();
                if (random.Next(100) < 2)
                {
                    _velocity = new Vector2(random.Next(-1, 2), random.Next(-1, 2));
                }

                _position += _velocity * _speed;

                // Keep the ant within the screen bounds
                _position = Vector2.Clamp(_position, Vector2.Zero, new Vector2(screenWidth - _size, screenHeight - _size));

                // Update rotation based on velocity
                _rotation = (float)Math.Atan2(_velocity.Y, _velocity.X);
            }

            _rotation = (float)Math.Atan2(_velocity.Y, _velocity.X);
        }

        public void PickUpFood(Food food)
        {
            _isCarryingFood = true;
            _homePosition = food.Position;
            food.PickedUp();
        }

        public void DropFood()
        {
            _isCarryingFood = false;
            _homePosition = _position;
        }
    }

    public class Food
    {
        private Vector2 _position;
        private readonly int _size;
        private bool _isPickedUp;

        public Vector2 Position { get { return _position; } }
        public bool IsPickedUp { get { return _isPickedUp; } }

        public Food(Vector2 position, int size)
        {
            _position = position;
            _size = size;
            _isPickedUp = false;
        }

        public void PickedUp()
        {
            _isPickedUp = true;
        }
    }

    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new Game1())
                game.Run();
        }
    }
}