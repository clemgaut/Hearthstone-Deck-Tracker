#region

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using HearthDb.Enums;
using Hearthstone_Deck_Tracker.Enums;
using Hearthstone_Deck_Tracker.Hearthstone;
using Hearthstone_Deck_Tracker.Hearthstone.Entities;
using Hearthstone_Deck_Tracker.Replay.Controls;
using Hearthstone_Deck_Tracker.Utility.Logging;
using static System.Windows.Visibility;
using static HearthDb.Enums.GameTag;
using static Hearthstone_Deck_Tracker.Replay.KeyPointType;

#endregion

namespace Hearthstone_Deck_Tracker.Replay
{
    /// <summary>
    /// Interaction logic for ReplayViewer.xaml
    /// </summary>
    public class ReplayViewerNoUi
    {
        private readonly List<int> _collapsedTurns;
        private readonly bool _initialized;
        private readonly List<int> _showAllTurns;
        public ReplayKeyPoint _currentGameState;
        private int _opponentController;
        private int _playerController;
        public List<ReplayKeyPoint> Replay;

        public ReplayViewerNoUi()
        {
            /*InitializeComponent();
			Height = Config.Instance.ReplayWindowHeight;
			Width = Config.Instance.ReplayWindowWidth;
			if(Config.Instance.ReplayWindowLeft.HasValue)
				Left = Config.Instance.ReplayWindowLeft.Value;
			if(Config.Instance.ReplayWindowTop.HasValue)
				Top = Config.Instance.ReplayWindowTop.Value;

			var titleBarCorners = new[]
			{
				new System.Drawing.Point((int)Left + 5, (int)Top + 5),
				new System.Drawing.Point((int)(Left + Width) - 5, (int)Top + 5),
				new System.Drawing.Point((int)Left + 5, (int)(Top + TitlebarHeight) - 5),
				new System.Drawing.Point((int)(Left + Width) - 5, (int)(Top + TitlebarHeight) - 5)
			};
			if(!Screen.AllScreens.Any(s => titleBarCorners.Any(c => s.WorkingArea.Contains(c))))
			{
				Top = 100;
				Left = 100;
			}*/
            _collapsedTurns = new List<int>();
            _showAllTurns = new List<int>();
            /*CheckBoxAttack.IsChecked = Config.Instance.ReplayViewerShowAttack;
			CheckBoxDeath.IsChecked = Config.Instance.ReplayViewerShowDeath;
			CheckBoxDiscard.IsChecked = Config.Instance.ReplayViewerShowDiscard;
			CheckBoxDraw.IsChecked = Config.Instance.ReplayViewerShowDraw;
			CheckBoxHeroPower.IsChecked = Config.Instance.ReplayViewerShowHeroPower;
			CheckBoxPlay.IsChecked = Config.Instance.ReplayViewerShowPlay;
			CheckBoxSecret.IsChecked = Config.Instance.ReplayViewerShowSecret;
			CheckBoxSummon.IsChecked = Config.Instance.ReplayViewerShowSummon;*/
            _initialized = true;
        }

        private Entity PlayerEntity
        {
            get { return _currentGameState?.Data.First(x => x.IsPlayer); }
        }

        private Entity OpponentEntity
        {
            get { return _currentGameState?.Data.First(x => x.HasTag(PLAYER_ID) && !x.IsPlayer); }
        }

        private Entity GetHero(int controller)
        {
            var heroEntityId = controller == _playerController
                                   ? PlayerEntity.GetTag(HERO_ENTITY) : OpponentEntity.GetTag(HERO_ENTITY);

            return _currentGameState.Data.FirstOrDefault(x => x.Id == heroEntityId) ?? new Entity();
        }

        private Entity GetEntity(IEnumerable<Entity> zone, int index)
            => zone.FirstOrDefault(x => x.HasTag(ZONE_POSITION) && x.GetTag(ZONE_POSITION) == index + 1);

        public Entity OpponentCardPlayed
        {
            get
            {
                var entity = _currentGameState.Data.FirstOrDefault(e => e.Id == _currentGameState.Id);
                if (entity?.IsControlledBy(_opponentController) == true
                   && (_currentGameState.Type == Play || _currentGameState.Type == PlaySpell))
                {
                    entity.SetCardCount(0);
                    return entity;
                }
                return null;
            }
        }

        public Entity PlayerCardPlayed
        {
            get
            {
                var entity = _currentGameState.Data.FirstOrDefault(e => e.Id == _currentGameState.Id);
                if (entity?.IsControlledBy(_playerController) == true
                   && (_currentGameState.Type == Play || _currentGameState.Type == PlaySpell))
                {
                    entity.SetCardCount(0);
                    return entity;
                }
                return null;
            }
        }

        public List<ReplayKeyPoint> KeyPoints => Replay;

        public BitmapImage PlayerHeroImage
        {
            get
            {
                if (!Enum.GetNames(typeof(HeroClass)).Contains(PlayerHero))
                    return new BitmapImage();
                return new BitmapImage(new Uri($"../Resources/{PlayerHero.ToLower()}_small.png", UriKind.Relative));
            }
        }

        public string PlayerName => _currentGameState == null ? string.Empty : PlayerEntity.Name;

        public string PlayerHero
        {
            get
            {
                if (_currentGameState == null)
                    return string.Empty;
                var cardId = GetHero(_playerController).CardId;
                return cardId == null ? null : Database.GetHeroNameFromId(cardId);
            }
        }

        public int PlayerHealth
        {
            get
            {
                if (_currentGameState == null)
                    return 30;
                var hero = GetHero(_playerController);
                return hero.GetTag(HEALTH) - hero.GetTag(DAMAGE);
            }
        }

        public int PlayerArmor => _currentGameState == null ? 0 : GetHero(_playerController).GetTag(ARMOR);

        public Visibility PlayerArmorVisibility => PlayerArmor > 0 ? Visible : Hidden;

        public int PlayerAttack => _currentGameState == null ? 0 : GetHero(_playerController).GetTag(ATK);

        public Visibility PlayerAttackVisibility => PlayerAttack > 0 ? Visible : Hidden;

        public IEnumerable<Entity> PlayerHand => _currentGameState?.Data.Where(x => x.IsInZone(Zone.HAND) && x.IsControlledBy(_playerController)) ?? new List<Entity>();

        public IEnumerable<Entity> PlayerBoard => _currentGameState?.Data.Where(
                                                                                x =>
                                                                                x.IsInZone(Zone.PLAY) && x.IsControlledBy(_playerController) && x.HasTag(HEALTH)
                                                                                && !string.IsNullOrEmpty(x.CardId) && !x.CardId.Contains("HERO")) ?? new List<Entity>();

        public string OpponentName => _currentGameState == null ? string.Empty : OpponentEntity.Name;

        public string OpponentHero
        {
            get
            {
                if (_currentGameState == null)
                    return null;
                var cardId = GetHero(_opponentController).CardId;
                return cardId == null ? null : Database.GetHeroNameFromId(cardId);
            }
        }

        public int OpponentHealth
        {
            get
            {
                if (_currentGameState == null)
                    return 30;
                var hero = GetHero(_opponentController);
                return hero.GetTag(HEALTH) - hero.GetTag(DAMAGE);
            }
        }

        public int OpponentArmor => _currentGameState == null ? 0 : GetHero(_opponentController).GetTag(ARMOR);

        public Visibility OpponentArmorVisibility => OpponentArmor > 0 ? Visible : Hidden;

        public int OpponentAttack => _currentGameState == null ? 0 : GetHero(_opponentController).GetTag(ATK);

        public Visibility OpponentAttackVisibility => OpponentAttack > 0 ? Visible : Hidden;

        public IEnumerable<Entity> OpponentHand => _currentGameState?.Data.Where(x => x.IsInZone(Zone.HAND) && x.IsControlledBy(_opponentController)) ?? new List<Entity>();

        public IEnumerable<Entity> OpponentBoard => _currentGameState?.Data.Where(
                                                                                  x =>
                                                                                  x.IsInZone(Zone.PLAY) && x.IsControlledBy(_opponentController) && x.HasTag(HEALTH)
                                                                                  && !string.IsNullOrEmpty(x.CardId) && !x.CardId.Contains("HERO")) ?? new List<Entity>();

        public Entity OpponentBoardHero => GetHero(_opponentController);
        public Entity OpponentBoard0 => GetEntity(OpponentBoard, 0);
        public Entity OpponentBoard1 => GetEntity(OpponentBoard, 1);
        public Entity OpponentBoard2 => GetEntity(OpponentBoard, 2);
        public Entity OpponentBoard3 => GetEntity(OpponentBoard, 3);
        public Entity OpponentBoard4 => GetEntity(OpponentBoard, 4);
        public Entity OpponentBoard5 => GetEntity(OpponentBoard, 5);
        public Entity OpponentBoard6 => GetEntity(OpponentBoard, 6);
        public Entity PlayerBoardHero => GetHero(_playerController);
        public Entity PlayerBoard0 => GetEntity(PlayerBoard, 0);
        public Entity PlayerBoard1 => GetEntity(PlayerBoard, 1);
        public Entity PlayerBoard2 => GetEntity(PlayerBoard, 2);
        public Entity PlayerBoard3 => GetEntity(PlayerBoard, 3);
        public Entity PlayerBoard4 => GetEntity(PlayerBoard, 4);
        public Entity PlayerBoard5 => GetEntity(PlayerBoard, 5);
        public Entity PlayerBoard6 => GetEntity(PlayerBoard, 6);
        public Entity PlayerCard0 => GetEntity(PlayerHand, 0);
        public Entity PlayerCard1 => GetEntity(PlayerHand, 1);
        public Entity PlayerCard2 => GetEntity(PlayerHand, 2);
        public Entity PlayerCard3 => GetEntity(PlayerHand, 3);
        public Entity PlayerCard4 => GetEntity(PlayerHand, 4);
        public Entity PlayerCard5 => GetEntity(PlayerHand, 5);
        public Entity PlayerCard6 => GetEntity(PlayerHand, 6);
        public Entity PlayerCard7 => GetEntity(PlayerHand, 7);
        public Entity PlayerCard8 => GetEntity(PlayerHand, 8);
        public Entity PlayerCard9 => GetEntity(PlayerHand, 9);
        public Entity OpponentCard0 => GetEntity(OpponentHand, 0);
        public Entity OpponentCard1 => GetEntity(OpponentHand, 1);
        public Entity OpponentCard2 => GetEntity(OpponentHand, 2);
        public Entity OpponentCard3 => GetEntity(OpponentHand, 3);
        public Entity OpponentCard4 => GetEntity(OpponentHand, 4);
        public Entity OpponentCard5 => GetEntity(OpponentHand, 5);
        public Entity OpponentCard6 => GetEntity(OpponentHand, 6);
        public Entity OpponentCard7 => GetEntity(OpponentHand, 7);
        public Entity OpponentCard8 => GetEntity(OpponentHand, 8);
        public Entity OpponentCard9 => GetEntity(OpponentHand, 9);

        public Entity PlayerWeapon
        {
            get
            {
                if (_currentGameState == null)
                    return null;
                var weaponId = PlayerEntity.GetTag(WEAPON);
                if (weaponId == 0)
                    return null;
                var entity = _currentGameState.Data.FirstOrDefault(x => x.Id == weaponId);
                entity?.SetCardCount(entity.GetTag(DURABILITY) - entity.GetTag(DAMAGE));
                return entity;
            }
        }

        public Entity OpponentWeapon
        {
            get
            {
                if (_currentGameState == null)
                    return null;
                var weaponId = OpponentEntity.GetTag(WEAPON);
                if (weaponId == 0)
                    return null;
                var entity = _currentGameState.Data.FirstOrDefault(x => x.Id == weaponId);
                entity?.SetCardCount(entity.GetTag(DURABILITY) - entity.GetTag(DAMAGE));
                return entity;
            }
        }

        public Visibility OpponentSecretVisibility => OpponentSecrets.Any() ? Visible : Collapsed;

        private IEnumerable<Entity> OpponentSecrets => _currentGameState?.Data.Where(
                                                                                     x =>
                                                                                     x.GetTag(ZONE) == (int)Zone.SECRET && x.IsControlledBy(_opponentController)) ?? new List<Entity>();

        private IEnumerable<Entity> PlayerSecrets => _currentGameState?.Data.Where(x => x.GetTag(ZONE) == (int)Zone.SECRET && x.IsControlledBy(_playerController)) ?? new List<Entity>();

        public Visibility PlayerSecretVisibility => PlayerSecrets.Any() ? Visible : Collapsed;

        public Entity OpponentSecret0 => OpponentSecrets.Any() ? OpponentSecrets.ToArray()[0] : null;
        public Entity OpponentSecret1 => OpponentSecrets.Count() > 1 ? OpponentSecrets.ToArray()[1] : null;
        public Entity OpponentSecret2 => OpponentSecrets.Count() > 2 ? OpponentSecrets.ToArray()[2] : null;
        public Entity OpponentSecret3 => OpponentSecrets.Count() > 3 ? OpponentSecrets.ToArray()[3] : null;
        public Entity OpponentSecret4 => OpponentSecrets.Count() > 4 ? OpponentSecrets.ToArray()[4] : null;
        public Entity PlayerSecret0 => PlayerSecrets.Any() ? PlayerSecrets.ToArray()[0] : null;
        public Entity PlayerSecret1 => PlayerSecrets.Count() > 1 ? PlayerSecrets.ToArray()[1] : null;
        public Entity PlayerSecret2 => PlayerSecrets.Count() > 2 ? PlayerSecrets.ToArray()[2] : null;
        public Entity PlayerSecret3 => PlayerSecrets.Count() > 3 ? PlayerSecrets.ToArray()[3] : null;
        public Entity PlayerSecret4 => PlayerSecrets.Count() > 4 ? PlayerSecrets.ToArray()[4] : null;

        public SolidColorBrush PlayerHealthTextColor
        {
            get
            {
                if (_currentGameState == null)
                    return new SolidColorBrush(Colors.White);
                var hero =
                    _currentGameState.Data.FirstOrDefault(
                                                          x =>
                                                          x.IsControlledBy(_playerController) && !string.IsNullOrEmpty(x.CardId)
                                                          && x.CardId.Contains("HERO"));
                return new SolidColorBrush((hero != null && hero.GetTag(DAMAGE) > 0) ? Colors.Red : Colors.White);
            }
        }

        public SolidColorBrush OpponentHealthTextColor
        {
            get
            {
                if (_currentGameState == null)
                    return new SolidColorBrush(Colors.White);
                var hero =
                    _currentGameState.Data.FirstOrDefault(
                                                          x =>
                                                          x.IsControlledBy(_opponentController) && !string.IsNullOrEmpty(x.CardId)
                                                          && x.CardId.Contains("HERO"));
                return new SolidColorBrush((hero != null && hero.GetTag(DAMAGE) > 0) ? Colors.Red : Colors.White);
            }
        }

        public string PlayerMana
        {
            get
            {
                if (_currentGameState == null)
                    return "0/0";
                var total = PlayerEntity.GetTag(RESOURCES);
                var current = total - PlayerEntity.GetTag(RESOURCES_USED);
                return current + "/" + total;
            }
        }

        public string OpponentMana
        {
            get
            {
                if (_currentGameState == null)
                    return "0/0";
                var total = OpponentEntity.GetTag(RESOURCES);
                var current = total - OpponentEntity.GetTag(RESOURCES_USED);
                return current + "/" + total;
            }
        }
        public void LoadNoUI(List<ReplayKeyPoint> replay)
        {
            if (replay == null || replay.Count == 0)
                return;
            Replay = replay;
            _currentGameState = Replay.FirstOrDefault(r => r.Data.Any(x => x.HasTag(PLAYER_ID)));
            if (_currentGameState == null)
            {
                Log.Error("No player entity found.");
                return;
            }
            _playerController = PlayerEntity.GetTag(CONTROLLER);
            _opponentController = OpponentEntity.GetTag(CONTROLLER);
        }

        public void ReloadKeypoints()
        {
            LoadNoUI(Replay);
        }
    }
}