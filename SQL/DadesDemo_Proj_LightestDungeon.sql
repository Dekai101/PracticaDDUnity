SET FOREIGN_KEY_CHECKS = 0;

DELETE FROM ItemEffect;
DELETE FROM SkillEffect;
DELETE FROM EntitySkill;
DELETE FROM LootEntry;
DELETE FROM LootTable;
DELETE FROM Enemy;
DELETE FROM Player;
DELETE FROM Item;
DELETE FROM Effect;
DELETE FROM Skill;
DELETE FROM Status;
DELETE FROM Statistic;
DELETE FROM Entity;

SET FOREIGN_KEY_CHECKS = 1;

-- =====================================================
-- 1️⃣ STATISTICS
-- =====================================================
INSERT INTO Statistic (name) VALUES
('hp'),
('energy'),
('defense'),
('attack'),
('speed'),
('crit_chance'),
('crit_damage'),
('accuracy_multiplier');

-- =====================================================
-- 2️⃣ STATUS
-- =====================================================
INSERT INTO Status (name, max_level, description, scaling_formula) VALUES
('Sangrado',    3, 'Pierde vida cada turno.',                             'HP * 0.05 * level'),
('Envenenado',  3, 'Pierde HP cada turno. Debuff apilable.',              'HP * 0.04 * level'),
('Fortalecido', 3, 'Aumenta el ataque del objetivo. Buff temporal.',      'Attack * 0.15 * level'),
('Aturdido',    2, 'Probabilidad de perder el turno. Debuff de control.', '0.40 + (0.20 * level)');

-- =====================================================
-- 3️⃣ SKILLS
-- =====================================================
INSERT INTO Skill
(name, description, energy_cost, accuracy, hits, target_type, is_aoe, is_passive, image_thumb)
VALUES
-- Skills originales
('Aquatic Blessing', 'Restaura HP y Energía a un aliado.',               40, 0.90, 1, 'ALLY',  false, false, '/img/aquatic.png'),
('Iron Body',        'Aumenta la defensa del usuario.',                   30, 1.00, 1, 'SELF',  false, false, '/img/iron.png'),
('Blood Rain',       'Ataque en área con posibilidad de Sangrado.',       70, 0.95, 1, 'ENEMY', true,  false, '/img/blood.png'),
-- Nuevas skills — DAÑO
('Shadow Strike',    'Golpe rápido que ignora parte de la defensa.',      25, 0.95, 1, 'ENEMY', false, false, '/img/shadow_strike.png'),
('Meteor Crash',     'Impacto masivo que daña a todos los enemigos.',     80, 0.85, 1, 'ENEMY', true,  false, '/img/meteor.png'),
('Poison Dart',      'Disparo que aplica Envenenado al objetivo.',        20, 0.90, 1, 'ENEMY', false, false, '/img/poison_dart.png'),
('Twin Slash',       'Dos golpes rápidos consecutivos al mismo objetivo.',35, 0.90, 2, 'ENEMY', false, false, '/img/twin_slash.png'),
('Soul Drain',       'Absorbe HP del enemigo y se cura en un 50%.',       50, 0.85, 1, 'ENEMY', false, false, '/img/soul_drain.png'),
-- Nuevas skills — SOPORTE
('Battle Cry',       'Aplica Fortalecido a todos los aliados.',           45, 1.00, 1, 'ALLY',  true,  false, '/img/battle_cry.png'),
('Healing Wave',     'Cura moderada a todos los aliados.',                60, 1.00, 1, 'ALLY',  true,  false, '/img/healing_wave.png'),
('Barrier Shield',   'Aplica un escudo de defensa al aliado objetivo.',   40, 1.00, 1, 'ALLY',  false, false, '/img/barrier.png'),
('Revive',           'Revive a un aliado caído con el 30% de HP.',        90, 1.00, 1, 'ALLY',  false, false, '/img/revive.png'),
-- Nuevas skills — CONTROL
('Stun Smash',       'Golpe fuerte con posibilidad de aturdir.',          45, 0.88, 1, 'ENEMY', false, false, '/img/stun_smash.png'),
('Frost Nova',       'Ráfaga de hielo que aturde a todos los enemigos.',  75, 0.80, 1, 'ENEMY', true,  false, '/img/frost_nova.png'),
-- Nuevas skills — PASIVAS
('Evasion Mastery',  'Aumenta la precisión base del portador.',            0, 1.00, 0, 'SELF',  false, true,  '/img/evasion.png'),
('Berserker Rage',   'Al bajar del 30% HP, aumenta el ataque automáticamente.', 0, 1.00, 0, 'SELF', false, true, '/img/berserk.png');

-- =====================================================
-- 4️⃣ EFECTOS
-- =====================================================
-- Convención min_flat_power / max_flat_power:
--   NULL  → efecto puramente porcentual (usa stat_multiplier)
--   valor → poder base fijo que el código escalará con nivel + stat del personaje
-- Skills de soporte/buff y efectos de estado siempre tienen flat = NULL.
-- Skills de daño directo: mezcla según su naturaleza (ver comentarios).
-- Items de armadura/accesorio: siempre porcentuales (flat = NULL).
-- Armas: ligeras/rápidas → fijo; pesadas/mágicas → porcentual.

-- Capturar IDs de estadísticas
SET @HP       = (SELECT id FROM Statistic WHERE name = 'hp');
SET @Energy   = (SELECT id FROM Statistic WHERE name = 'energy');
SET @Defense  = (SELECT id FROM Statistic WHERE name = 'defense');
SET @Attack   = (SELECT id FROM Statistic WHERE name = 'attack');
SET @Speed    = (SELECT id FROM Statistic WHERE name = 'speed');

-- Capturar IDs de estados
SET @Sangrado    = (SELECT id FROM Status WHERE name = 'Sangrado');
SET @Envenenado  = (SELECT id FROM Status WHERE name = 'Envenenado');
SET @Fortalecido = (SELECT id FROM Status WHERE name = 'Fortalecido');
SET @Aturdido    = (SELECT id FROM Status WHERE name = 'Aturdido');

-- -------------------------------------------------------
-- EFECTOS DE SKILLS ORIGINALES
-- -------------------------------------------------------

-- Aquatic Blessing: curación porcentual de HP
INSERT INTO Effect (stat_id, stat_multiplier, status_id, min_flat_power, max_flat_power, effect_level, probability, duration_turns)
VALUES (@HP, 1.30, NULL, NULL, NULL, NULL, 1.0, 1);

-- Aquatic Blessing: restaura energía porcentual
INSERT INTO Effect (stat_id, stat_multiplier, status_id, min_flat_power, max_flat_power, effect_level, probability, duration_turns)
VALUES (@Energy, 1.10, NULL, NULL, NULL, NULL, 1.0, 1);

-- Iron Body: buff porcentual de defensa
INSERT INTO Effect (stat_id, stat_multiplier, status_id, min_flat_power, max_flat_power, effect_level, probability, duration_turns)
VALUES (@Defense, 1.20, NULL, NULL, NULL, NULL, 1.0, 3);

-- Blood Rain: DAÑO FIJO — ataque en área (base moderada, escalará con ataque + nivel)
INSERT INTO Effect (stat_id, stat_multiplier, status_id, min_flat_power, max_flat_power, effect_level, probability, duration_turns)
VALUES (@Attack, NULL, NULL, 18, 24, NULL, 1.0, 0);

-- Blood Rain: aplica Sangrado lv1 (sin poder base, escala vía scaling_formula del Status)
INSERT INTO Effect (stat_id, stat_multiplier, status_id, min_flat_power, max_flat_power, effect_level, probability, duration_turns)
VALUES (NULL, NULL, @Sangrado, NULL, NULL, 1, 0.15, 3);

-- -------------------------------------------------------
-- EFECTOS DE NUEVAS SKILLS
-- -------------------------------------------------------

-- Shadow Strike: DAÑO FIJO + multiplicador leve (golpe físico ágil)
INSERT INTO Effect (stat_id, stat_multiplier, status_id, min_flat_power, max_flat_power, effect_level, probability, duration_turns)
VALUES (@Attack, 1.15, NULL, 10, 14, NULL, 1.00, 0);

-- Meteor Crash: DAÑO FIJO alto — impacto masivo AOE (sin multiplicador, el poder base es lo relevante)
INSERT INTO Effect (stat_id, stat_multiplier, status_id, min_flat_power, max_flat_power, effect_level, probability, duration_turns)
VALUES (@Attack, NULL, NULL, 22, 30, NULL, 1.00, 0);

-- Poison Dart: sin daño propio (todo el daño viene del estado Envenenado)
INSERT INTO Effect (stat_id, stat_multiplier, status_id, min_flat_power, max_flat_power, effect_level, probability, duration_turns)
VALUES (NULL, NULL, @Envenenado, NULL, NULL, 1, 1.00, 4);

-- Twin Slash: DAÑO FIJO por hit — dos golpes rápidos, base baja individual
INSERT INTO Effect (stat_id, stat_multiplier, status_id, min_flat_power, max_flat_power, effect_level, probability, duration_turns)
VALUES (@Attack, NULL, NULL, 8, 11, NULL, 1.00, 0);

-- Soul Drain: DAÑO FIJO — absorción oscura
INSERT INTO Effect (stat_id, stat_multiplier, status_id, min_flat_power, max_flat_power, effect_level, probability, duration_turns)
VALUES (@Attack, NULL, NULL, 14, 19, NULL, 1.00, 0);

-- Soul Drain: curación porcentual del HP (la lógica de "50% del daño hecho" va en código)
INSERT INTO Effect (stat_id, stat_multiplier, status_id, min_flat_power, max_flat_power, effect_level, probability, duration_turns)
VALUES (@HP, 1.50, NULL, NULL, NULL, NULL, 1.00, 1);

-- Battle Cry: aplica estado Fortalecido, sin poder base numérico
INSERT INTO Effect (stat_id, stat_multiplier, status_id, min_flat_power, max_flat_power, effect_level, probability, duration_turns)
VALUES (NULL, NULL, @Fortalecido, NULL, NULL, 2, 1.00, 3);

-- Healing Wave: curación porcentual AOE
INSERT INTO Effect (stat_id, stat_multiplier, status_id, min_flat_power, max_flat_power, effect_level, probability, duration_turns)
VALUES (@HP, 1.25, NULL, NULL, NULL, NULL, 1.00, 1);

-- Barrier Shield: buff porcentual de defensa
INSERT INTO Effect (stat_id, stat_multiplier, status_id, min_flat_power, max_flat_power, effect_level, probability, duration_turns)
VALUES (@Defense, 1.30, NULL, NULL, NULL, NULL, 1.00, 2);

-- Revive: restaura HP porcentual al revivir
INSERT INTO Effect (stat_id, stat_multiplier, status_id, min_flat_power, max_flat_power, effect_level, probability, duration_turns)
VALUES (@HP, 1.30, NULL, NULL, NULL, NULL, 1.00, 1);

-- Stun Smash: DAÑO FIJO — golpe contundente pesado
INSERT INTO Effect (stat_id, stat_multiplier, status_id, min_flat_power, max_flat_power, effect_level, probability, duration_turns)
VALUES (@Attack, NULL, NULL, 16, 22, NULL, 1.00, 0);

-- Stun Smash: aplica Aturdido lv1 (sin poder base)
INSERT INTO Effect (stat_id, stat_multiplier, status_id, min_flat_power, max_flat_power, effect_level, probability, duration_turns)
VALUES (NULL, NULL, @Aturdido, NULL, NULL, 1, 0.30, 1);

-- Frost Nova: DAÑO FIJO menor — la skill prioriza el control sobre el daño
INSERT INTO Effect (stat_id, stat_multiplier, status_id, min_flat_power, max_flat_power, effect_level, probability, duration_turns)
VALUES (@Attack, NULL, NULL, 10, 14, NULL, 1.00, 0);

-- Frost Nova: aplica Aturdido lv1 50%
INSERT INTO Effect (stat_id, stat_multiplier, status_id, min_flat_power, max_flat_power, effect_level, probability, duration_turns)
VALUES (NULL, NULL, @Aturdido, NULL, NULL, 1, 0.50, 1);

-- Evasion Mastery (pasiva): buff porcentual de velocidad
INSERT INTO Effect (stat_id, stat_multiplier, status_id, min_flat_power, max_flat_power, effect_level, probability, duration_turns)
VALUES (@Speed, 1.20, NULL, NULL, NULL, NULL, 1.00, 0);

-- Berserker Rage (pasiva): buff porcentual de ataque
INSERT INTO Effect (stat_id, stat_multiplier, status_id, min_flat_power, max_flat_power, effect_level, probability, duration_turns)
VALUES (@Attack, 1.40, NULL, NULL, NULL, NULL, 1.00, 0);

-- -------------------------------------------------------
-- EFECTOS DE ITEMS — HEAD (todos porcentuales, flat = NULL)
-- -------------------------------------------------------
INSERT INTO Effect (stat_id, stat_multiplier, status_id, min_flat_power, max_flat_power, effect_level, probability, duration_turns) VALUES
(@Defense, 1.10, NULL, NULL, NULL, 1, 1.0, 0),   -- Iron Helm         +10% def
(@Defense, 1.20, NULL, NULL, NULL, 1, 1.0, 0),   -- Knight Visor      +20% def
(@Speed,   1.15, NULL, NULL, NULL, 1, 1.0, 0),   -- Shadow Hood       +15% spd
(@Energy,  1.25, NULL, NULL, NULL, 1, 1.0, 0),   -- Arcane Crown      +25% energy
(@Speed,   1.10, NULL, NULL, NULL, 1, 1.0, 0),   -- Ranger Cap        +10% spd
(@HP,      1.15, NULL, NULL, NULL, 1, 1.0, 0),   -- Vampire Mask      +15% hp
(@Defense, 1.35, NULL, NULL, NULL, 1, 1.0, 0),   -- Golem Skull Plate +35% def
(NULL,     NULL, NULL, NULL, NULL, 1, 1.0, 0);   -- Blessed Tiara     (se actualiza abajo)

-- Blessed Tiara: +20% speed como proxy de accuracy
UPDATE Effect SET stat_id = @Speed, stat_multiplier = 1.20
WHERE id = LAST_INSERT_ID();

-- -------------------------------------------------------
-- EFECTOS DE ITEMS — CHEST (todos porcentuales, flat = NULL)
-- -------------------------------------------------------
INSERT INTO Effect (stat_id, stat_multiplier, status_id, min_flat_power, max_flat_power, effect_level, probability, duration_turns) VALUES
(@Defense, 1.15, NULL, NULL, NULL, 1, 1.0, 0),   -- Iron Chestplate   +15% def
(@Defense, 1.25, NULL, NULL, NULL, 1, 1.0, 0),   -- Battle Armor      +25% def
(@Speed,   1.12, NULL, NULL, NULL, 1, 1.0, 0),   -- Leather Vest      +12% spd
(@Energy,  1.30, NULL, NULL, NULL, 1, 1.0, 0),   -- Mage Robe         +30% energy
(@Speed,   1.18, NULL, NULL, NULL, 1, 1.0, 0),   -- Shadow Cloak      +18% spd
(@HP,      1.20, NULL, NULL, NULL, 1, 1.0, 0),   -- Blessed Vestment  +20% hp
(@Defense, 1.40, NULL, NULL, NULL, 1, 1.0, 0),   -- Golem Shell       +40% def
(@Energy,  1.25, NULL, NULL, NULL, 1, 1.0, 0);   -- Vampiric Coat     +25% energy

-- -------------------------------------------------------
-- EFECTOS DE ITEMS — LOWER (todos porcentuales, flat = NULL)
-- -------------------------------------------------------
INSERT INTO Effect (stat_id, stat_multiplier, status_id, min_flat_power, max_flat_power, effect_level, probability, duration_turns) VALUES
(@Defense, 1.08, NULL, NULL, NULL, 1, 1.0, 0),   -- Iron Greaves      +8%  def
(@Defense, 1.18, NULL, NULL, NULL, 1, 1.0, 0),   -- Knight Leggings   +18% def
(@Speed,   1.25, NULL, NULL, NULL, 1, 1.0, 0),   -- Swift Boots       +25% spd
(@Energy,  1.10, NULL, NULL, NULL, 1, 1.0, 0),   -- Mage Sandals      +10% energy
(@Speed,   1.20, NULL, NULL, NULL, 1, 1.0, 0),   -- Shadow Leggings   +20% spd
(@Speed,   1.15, NULL, NULL, NULL, 1, 1.0, 0),   -- Ranger Boots      +15% spd
(@Attack,  1.12, NULL, NULL, NULL, 1, 1.0, 0),   -- Golem Stompers    +12% atk
(@Defense, 1.15, NULL, NULL, NULL, 1, 1.0, 0);   -- Blessed Sandals   +15% def

-- -------------------------------------------------------
-- EFECTOS DE ITEMS — WEAPONS
-- Armas ligeras/rápidas → DAÑO FIJO (flat, sin multiplicador)
-- Armas pesadas/mágicas → PORCENTUAL (multiplicador, sin flat)
-- -------------------------------------------------------
INSERT INTO Effect (stat_id, stat_multiplier, status_id, min_flat_power, max_flat_power, effect_level, probability, duration_turns) VALUES
(@Attack, NULL, NULL,  6,    9,    1, 1.0, 0),   -- Short Sword          FIJO  base baja
(@Attack, NULL, NULL, 10,   14,    1, 1.0, 0),   -- Longsword            FIJO  estándar
(@Attack, NULL, NULL,  7,   10,    1, 1.0, 0),   -- Shadow Dagger        FIJO  ágil/rápida
(@Attack, 1.18, NULL, NULL, NULL,  1, 1.0, 0),   -- Poison Blade         PORC  (valor en veneno)
(@Attack, NULL, NULL, 16,   22,    1, 1.0, 0),   -- War Hammer           FIJO  pesado
(@Energy, 1.35, NULL, NULL, NULL,  1, 1.0, 0),   -- Arcane Staff         PORC  amplifica energía
(@HP,     1.10, NULL, NULL, NULL,  1, 1.0, 0),   -- Holy Wand            PORC  potencia curaciones
(@Attack, NULL, NULL,  9,   13,    1, 1.0, 0),   -- Longbow              FIJO  preciso
(@Attack, NULL, NULL, 12,   17,    1, 1.0, 0),   -- Crossbow             FIJO  potente
(@Attack, 1.30, NULL, NULL, NULL,  1, 1.0, 0),   -- Blood Scythe         PORC  épica, escala con atk
(@Attack, NULL, NULL, 20,   28,    1, 1.0, 0),   -- Stone Fist Gauntlet  FIJO  aplastante
(@Attack, NULL, NULL,  7,   10,    1, 1.0, 0);   -- Twin Blades          FIJO  por hit (igual que dagger)

-- Poison Blade: veneno on-hit 25% (el daño del veneno escala vía Status.scaling_formula)
INSERT INTO Effect (stat_id, stat_multiplier, status_id, min_flat_power, max_flat_power, effect_level, probability, duration_turns)
VALUES (NULL, NULL, @Envenenado, NULL, NULL, 1, 0.25, 3);

-- Blood Scythe: sangrado on-hit 20%
INSERT INTO Effect (stat_id, stat_multiplier, status_id, min_flat_power, max_flat_power, effect_level, probability, duration_turns)
VALUES (NULL, NULL, @Sangrado, NULL, NULL, 1, 0.20, 3);

-- -------------------------------------------------------
-- EFECTOS DE ITEMS — NON-CONSUMABLES (porcentuales o estados, flat = NULL)
-- -------------------------------------------------------

-- Vampire Ring: Sangrado lv2 on-hit 30%
INSERT INTO Effect (stat_id, stat_multiplier, status_id, min_flat_power, max_flat_power, effect_level, probability, duration_turns)
VALUES (NULL, NULL, @Sangrado, NULL, NULL, 2, 0.30, 3);

-- Poison Amulet: Envenenado lv2 on-hit 30%
INSERT INTO Effect (stat_id, stat_multiplier, status_id, min_flat_power, max_flat_power, effect_level, probability, duration_turns)
VALUES (NULL, NULL, @Envenenado, NULL, NULL, 2, 0.30, 4);

-- Battle Banner: Fortalecido lv1 a aliados 2 turnos
INSERT INTO Effect (stat_id, stat_multiplier, status_id, min_flat_power, max_flat_power, effect_level, probability, duration_turns)
VALUES (NULL, NULL, @Fortalecido, NULL, NULL, 1, 1.00, 2);

-- Lucky Charm, Stone Totem, Speed Anklet, Accuracy Lens, Crit Gem: porcentuales
INSERT INTO Effect (stat_id, stat_multiplier, status_id, min_flat_power, max_flat_power, effect_level, probability, duration_turns) VALUES
(@Speed,   1.15, NULL, NULL, NULL, 1, 1.0, 0),   -- Lucky Charm   +15% spd
(@Defense, 1.20, NULL, NULL, NULL, 1, 1.0, 0),   -- Stone Totem   +20% def
(@Speed,   1.20, NULL, NULL, NULL, 1, 1.0, 0),   -- Speed Anklet  +20% spd
(@Speed,   1.18, NULL, NULL, NULL, 1, 1.0, 0),   -- Accuracy Lens +18% spd
(@Attack,  1.10, NULL, NULL, NULL, 1, 1.0, 0);   -- Crit Gem      +10% atk

-- -------------------------------------------------------
-- EFECTOS DE ITEMS — CONSUMABLES
-- Porcentuales salvo indicación (los consumibles curan/buff basados en el stat máximo)
-- -------------------------------------------------------
INSERT INTO Effect (stat_id, stat_multiplier, status_id, min_flat_power, max_flat_power, effect_level, probability, duration_turns) VALUES
(@HP,     1.30, NULL,         NULL, NULL, 1, 1.0,  1),  -- Health Potion      +30% HP max
(@Energy, 1.30, NULL,         NULL, NULL, 1, 1.0,  1),  -- Energy Elixir      +30% Energy max
(NULL,    NULL, @Envenenado,  NULL, NULL, 0, 1.0,  0),  -- Antidote           cleanse veneno
(NULL,    NULL, @Fortalecido, NULL, NULL, 1, 1.0,  3),  -- Rage Brew          Fortalecido lv1
(NULL,    NULL, @Aturdido,    NULL, NULL, 1, 0.80, 1),  -- Smoke Bomb         Aturdido 80% AOE
(@HP,     1.60, NULL,         NULL, NULL, 1, 1.0,  1),  -- Greater Health Pot +60% HP max
(@Speed,  1.30, NULL,         NULL, NULL, 1, 1.0,  3),  -- Elixir of Speed    +30% spd temporal
(@HP,     1.50, NULL,         NULL, NULL, 1, 1.0,  1);  -- Phoenix Feather    +50% HP al revivir

-- =====================================================
-- 5️⃣ LINK SKILL ↔ EFFECT
-- =====================================================

-- Aquatic Blessing HP
INSERT INTO SkillEffect (skill_id, effect_id)
SELECT s.id, e.id FROM Skill s JOIN Effect e
  ON e.stat_id = @HP AND e.stat_multiplier = 1.30
WHERE s.name = 'Aquatic Blessing';

-- Aquatic Blessing Energy
INSERT INTO SkillEffect (skill_id, effect_id)
SELECT s.id, e.id FROM Skill s JOIN Effect e
  ON e.stat_id = @Energy AND e.stat_multiplier = 1.10
WHERE s.name = 'Aquatic Blessing';

-- Iron Body
INSERT INTO SkillEffect (skill_id, effect_id)
SELECT s.id, e.id FROM Skill s JOIN Effect e
  ON e.stat_id = @Defense AND e.stat_multiplier = 1.20
WHERE s.name = 'Iron Body';

-- Blood Rain daño (fijo: stat_multiplier IS NULL, min=18)
INSERT INTO SkillEffect (skill_id, effect_id)
SELECT s.id, e.id FROM Skill s JOIN Effect e
  ON e.stat_id = @Attack AND e.stat_multiplier IS NULL AND e.min_flat_power = 18 AND e.duration_turns = 0
WHERE s.name = 'Blood Rain';

-- Blood Rain Sangrado
INSERT INTO SkillEffect (skill_id, effect_id)
SELECT s.id, e.id FROM Skill s JOIN Effect e
  ON e.status_id = @Sangrado AND e.probability = 0.15
WHERE s.name = 'Blood Rain';

-- Shadow Strike (fijo + multiplicador: min=10)
INSERT INTO SkillEffect (skill_id, effect_id)
SELECT s.id, e.id FROM Skill s JOIN Effect e
  ON e.stat_id = @Attack AND e.stat_multiplier = 1.15 AND e.min_flat_power = 10 AND e.duration_turns = 0
WHERE s.name = 'Shadow Strike';

-- Meteor Crash (fijo: min=22)
INSERT INTO SkillEffect (skill_id, effect_id)
SELECT s.id, e.id FROM Skill s JOIN Effect e
  ON e.stat_id = @Attack AND e.stat_multiplier IS NULL AND e.min_flat_power = 22 AND e.duration_turns = 0
WHERE s.name = 'Meteor Crash';

-- Poison Dart
INSERT INTO SkillEffect (skill_id, effect_id)
SELECT s.id, e.id FROM Skill s JOIN Effect e
  ON e.status_id = @Envenenado AND e.probability = 1.00 AND e.duration_turns = 4
WHERE s.name = 'Poison Dart';

-- Twin Slash (fijo por hit: min=8)
INSERT INTO SkillEffect (skill_id, effect_id)
SELECT s.id, e.id FROM Skill s JOIN Effect e
  ON e.stat_id = @Attack AND e.stat_multiplier IS NULL AND e.min_flat_power = 8 AND e.duration_turns = 0
WHERE s.name = 'Twin Slash';

-- Soul Drain daño (fijo: min=14)
INSERT INTO SkillEffect (skill_id, effect_id)
SELECT s.id, e.id FROM Skill s JOIN Effect e
  ON e.stat_id = @Attack AND e.stat_multiplier IS NULL AND e.min_flat_power = 14 AND e.duration_turns = 0
WHERE s.name = 'Soul Drain';

-- Soul Drain curación
INSERT INTO SkillEffect (skill_id, effect_id)
SELECT s.id, e.id FROM Skill s JOIN Effect e
  ON e.stat_id = @HP AND e.stat_multiplier = 1.50 AND e.duration_turns = 1
WHERE s.name = 'Soul Drain';

-- Battle Cry
INSERT INTO SkillEffect (skill_id, effect_id)
SELECT s.id, e.id FROM Skill s JOIN Effect e
  ON e.status_id = @Fortalecido AND e.effect_level = 2 AND e.duration_turns = 3
WHERE s.name = 'Battle Cry';

-- Healing Wave
INSERT INTO SkillEffect (skill_id, effect_id)
SELECT s.id, e.id FROM Skill s JOIN Effect e
  ON e.stat_id = @HP AND e.stat_multiplier = 1.25 AND e.duration_turns = 1
WHERE s.name = 'Healing Wave';

-- Barrier Shield
INSERT INTO SkillEffect (skill_id, effect_id)
SELECT s.id, e.id FROM Skill s JOIN Effect e
  ON e.stat_id = @Defense AND e.stat_multiplier = 1.30 AND e.duration_turns = 2
WHERE s.name = 'Barrier Shield';

-- Revive
INSERT INTO SkillEffect (skill_id, effect_id)
SELECT s.id, e.id FROM Skill s JOIN Effect e
  ON e.stat_id = @HP AND e.stat_multiplier = 1.30 AND e.duration_turns = 1
WHERE s.name = 'Revive';

-- Stun Smash daño (fijo: min=16)
INSERT INTO SkillEffect (skill_id, effect_id)
SELECT s.id, e.id FROM Skill s JOIN Effect e
  ON e.stat_id = @Attack AND e.stat_multiplier IS NULL AND e.min_flat_power = 16 AND e.duration_turns = 0
WHERE s.name = 'Stun Smash';

-- Stun Smash aturdido
INSERT INTO SkillEffect (skill_id, effect_id)
SELECT s.id, e.id FROM Skill s JOIN Effect e
  ON e.status_id = @Aturdido AND e.probability = 0.30
WHERE s.name = 'Stun Smash';

-- Frost Nova daño (fijo: min=10)
INSERT INTO SkillEffect (skill_id, effect_id)
SELECT s.id, e.id FROM Skill s JOIN Effect e
  ON e.stat_id = @Attack AND e.stat_multiplier IS NULL AND e.min_flat_power = 10 AND e.duration_turns = 0
WHERE s.name = 'Frost Nova';

-- Frost Nova aturdido
INSERT INTO SkillEffect (skill_id, effect_id)
SELECT s.id, e.id FROM Skill s JOIN Effect e
  ON e.status_id = @Aturdido AND e.probability = 0.50
WHERE s.name = 'Frost Nova';

-- Evasion Mastery
INSERT INTO SkillEffect (skill_id, effect_id)
SELECT s.id, e.id FROM Skill s JOIN Effect e
  ON e.stat_id = @Speed AND e.stat_multiplier = 1.20 AND e.duration_turns = 0
WHERE s.name = 'Evasion Mastery';

-- Berserker Rage
INSERT INTO SkillEffect (skill_id, effect_id)
SELECT s.id, e.id FROM Skill s JOIN Effect e
  ON e.stat_id = @Attack AND e.stat_multiplier = 1.40 AND e.duration_turns = 0
WHERE s.name = 'Berserker Rage';

-- =====================================================
-- 6️⃣ PLAYERS
-- =====================================================
INSERT INTO Entity
(name, level, hp, hp_max, energy, energy_max, attack, defense, speed,
 crit_chance, crit_damage, accuracy_multiplier,
 image_thumb, image_full, description)
VALUES
('Hero Knight',     1, 10, 10, 10, 10, 10, 10, 10, 0.05, 1.5, 1.0, '/img/hero_t.png',   '/img/hero_f.png',   'Caballero equilibrado.'),
('Ocean Priestess', 1, 10, 10, 10, 10, 10, 10, 10, 0.05, 1.5, 1.0, '/img/priest_t.png', '/img/priest_f.png', 'Sacerdotisa marina.'),
('Arcane Mage',     1, 10, 10, 10, 10, 10, 10, 10, 0.05, 1.5, 1.0, '/img/mage_t.png',   '/img/mage_f.png',   'Mago versátil.'),
('Forest Ranger',   1, 10, 10, 10, 10, 10, 10, 10, 0.05, 1.5, 1.0, '/img/ranger_t.png', '/img/ranger_f.png', 'Explorador del bosque.');

INSERT INTO Player (entity_id, xp_points, skill_points)
SELECT id, 0, 3 FROM Entity
WHERE name IN ('Hero Knight', 'Ocean Priestess', 'Arcane Mage', 'Forest Ranger');

-- =====================================================
-- 7️⃣ ENEMIES
-- =====================================================
INSERT INTO Entity
(name, level, hp, hp_max, energy, energy_max, attack, defense, speed,
 crit_chance, crit_damage, accuracy_multiplier,
 image_thumb, image_full, description)
VALUES
('Goblin Berserker', 2, 25, 25, 10, 10, 15,  5, 12, 0.10, 1.5, 1.0, '/img/goblin_t.png',   '/img/goblin_f.png',   'Pequeño pero agresivo.'),
('Stone Golem',      3, 50, 50,  5,  5, 12, 20,  4, 0.05, 1.5, 0.9, '/img/golem_t.png',    '/img/golem_f.png',    'Tanque resistente.'),
('Vampire Lord',     5, 70, 70, 30, 30, 25, 12, 18, 0.20, 2.0, 1.1, '/img/vampire_t.png',  '/img/vampire_f.png',  'Dominador de la sangre.'),
('Dark Assassin',    4, 40, 40, 20, 20, 18,  8, 22, 0.15, 1.7, 1.0, '/img/assassin_t.png', '/img/assassin_f.png', 'Asesino veloz y mortal.');

-- (El trigger trg_enemy_create_loot_table crea la LootTable automáticamente)
INSERT INTO Enemy (entity_id, passiveId)
SELECT id, 0 FROM Entity
WHERE name IN ('Goblin Berserker', 'Stone Golem', 'Vampire Lord', 'Dark Assassin');

-- =====================================================
-- 8️⃣ ASIGNAR SKILLS A PERSONAJES
-- =====================================================

INSERT INTO EntitySkill (entity_id, skill_id)
SELECT e.id, s.id FROM Entity e, Skill s
WHERE e.name = 'Hero Knight'
  AND s.name IN ('Iron Body', 'Stun Smash', 'Battle Cry', 'Berserker Rage');

INSERT INTO EntitySkill (entity_id, skill_id)
SELECT e.id, s.id FROM Entity e, Skill s
WHERE e.name = 'Ocean Priestess'
  AND s.name IN ('Aquatic Blessing', 'Healing Wave', 'Barrier Shield', 'Revive');

INSERT INTO EntitySkill (entity_id, skill_id)
SELECT e.id, s.id FROM Entity e, Skill s
WHERE e.name = 'Arcane Mage'
  AND s.name IN ('Meteor Crash', 'Frost Nova', 'Poison Dart', 'Evasion Mastery');

INSERT INTO EntitySkill (entity_id, skill_id)
SELECT e.id, s.id FROM Entity e, Skill s
WHERE e.name = 'Forest Ranger'
  AND s.name IN ('Twin Slash', 'Shadow Strike', 'Evasion Mastery');

INSERT INTO EntitySkill (entity_id, skill_id)
SELECT e.id, s.id FROM Entity e, Skill s
WHERE e.name = 'Goblin Berserker'
  AND s.name IN ('Twin Slash', 'Berserker Rage');

INSERT INTO EntitySkill (entity_id, skill_id)
SELECT e.id, s.id FROM Entity e, Skill s
WHERE e.name = 'Stone Golem'
  AND s.name IN ('Iron Body', 'Stun Smash', 'Barrier Shield');

INSERT INTO EntitySkill (entity_id, skill_id)
SELECT e.id, s.id FROM Entity e, Skill s
WHERE e.name = 'Vampire Lord'
  AND s.name IN ('Blood Rain', 'Soul Drain', 'Battle Cry');

INSERT INTO EntitySkill (entity_id, skill_id)
SELECT e.id, s.id FROM Entity e, Skill s
WHERE e.name = 'Dark Assassin'
  AND s.name IN ('Shadow Strike', 'Twin Slash', 'Poison Dart', 'Evasion Mastery');

-- =====================================================
-- 9️⃣ ITEMS
-- =====================================================

-- ---- HEAD (8) ----
INSERT INTO Item (name, description, quality, consumable, max_uses, image_thumb, target_type, is_aoe) VALUES
('Iron Helm',         'Casco básico de hierro.',                 'COMMON',   false, NULL, '/img/iron_helm.png',    'SELF', false),
('Knight Visor',      'Visor de caballero con alta defensa.',    'UNCOMMON', false, NULL, '/img/knight_visor.png', 'SELF', false),
('Shadow Hood',       'Capucha oscura que mejora la velocidad.', 'UNCOMMON', false, NULL, '/img/shadow_hood.png',  'SELF', false),
('Arcane Crown',      'Corona mágica que potencia la energía.',  'RARE',     false, NULL, '/img/arcane_crown.png', 'SELF', false),
('Ranger Cap',        'Gorra ligera para arqueros.',             'COMMON',   false, NULL, '/img/ranger_cap.png',   'SELF', false),
('Vampire Mask',      'Máscara que otorga regeneración oscura.', 'RARE',     false, NULL, '/img/vamp_mask.png',    'SELF', false),
('Golem Skull Plate', 'Placa de piedra de un gólem derrotado.',  'EPIC',     false, NULL, '/img/golem_skull.png',  'SELF', false),
('Blessed Tiara',     'Tiara sagrada que aumenta la precisión.', 'RARE',     false, NULL, '/img/tiara.png',        'SELF', false);

-- ---- CHEST (8) ----
INSERT INTO Item (name, description, quality, consumable, max_uses, image_thumb, target_type, is_aoe) VALUES
('Iron Chestplate',  'Peto de hierro estándar.',                'COMMON',   false, NULL, '/img/iron_chest.png',   'SELF', false),
('Battle Armor',     'Armadura de batalla reforzada.',          'UNCOMMON', false, NULL, '/img/battle_armor.png', 'SELF', false),
('Leather Vest',     'Chaleco ligero para agilidad.',           'COMMON',   false, NULL, '/img/leather_vest.png', 'SELF', false),
('Mage Robe',        'Túnica mágica que amplifica la energía.', 'UNCOMMON', false, NULL, '/img/mage_robe.png',    'SELF', false),
('Shadow Cloak',     'Capa oscura que mejora la evasión.',      'RARE',     false, NULL, '/img/shadow_cloak.png', 'SELF', false),
('Blessed Vestment', 'Vestimenta sagrada con aura curativa.',   'RARE',     false, NULL, '/img/vestment.png',     'SELF', false),
('Golem Shell',      'Coraza de piedra mágica.',                'EPIC',     false, NULL, '/img/golem_shell.png',  'SELF', false),
('Vampiric Coat',    'Abrigo oscuro que drena energía.',        'EPIC',     false, NULL, '/img/vamp_coat.png',    'SELF', false);

-- ---- LOWER (8) ----
INSERT INTO Item (name, description, quality, consumable, max_uses, image_thumb, target_type, is_aoe) VALUES
('Iron Greaves',    'Grebas de hierro básicas.',                 'COMMON',   false, NULL, '/img/iron_greaves.png',    'SELF', false),
('Knight Leggings', 'Mallas de caballero resistentes.',          'UNCOMMON', false, NULL, '/img/knight_legs.png',     'SELF', false),
('Swift Boots',     'Botas ligeras de alta velocidad.',          'UNCOMMON', false, NULL, '/img/swift_boots.png',     'SELF', false),
('Mage Sandals',    'Sandalias que canalizan magia.',            'COMMON',   false, NULL, '/img/mage_sandals.png',    'SELF', false),
('Shadow Leggings', 'Pantalones de sigilo.',                     'RARE',     false, NULL, '/img/shadow_legs.png',     'SELF', false),
('Ranger Boots',    'Botas de explorador para terreno difícil.', 'UNCOMMON', false, NULL, '/img/ranger_boots.png',    'SELF', false),
('Golem Stompers',  'Pezuñas de piedra con impacto sísmico.',    'EPIC',     false, NULL, '/img/golem_stomp.png',     'SELF', false),
('Blessed Sandals', 'Sandalias sagradas que mejoran la crit.',   'RARE',     false, NULL, '/img/blessed_sandals.png', 'SELF', false);

-- ---- WEAPONS (12) ----
INSERT INTO Item (name, description, quality, consumable, max_uses, image_thumb, target_type, is_aoe) VALUES
('Short Sword',         'Espada corta de inicio.',                    'COMMON',   false, NULL, '/img/short_sword.png',  'SELF', false),
('Longsword',           'Espada larga de caballero.',                 'UNCOMMON', false, NULL, '/img/longsword.png',    'SELF', false),
('Shadow Dagger',       'Daga rápida del asesino.',                   'UNCOMMON', false, NULL, '/img/shadow_dagger.png','SELF', false),
('Poison Blade',        'Daga impregnada de veneno.',                 'RARE',     false, NULL, '/img/poison_blade.png', 'SELF', false),
('War Hammer',          'Martillo de guerra pesado.',                 'UNCOMMON', false, NULL, '/img/war_hammer.png',   'SELF', false),
('Arcane Staff',        'Bastón que amplifica hechizos.',             'RARE',     false, NULL, '/img/arcane_staff.png', 'SELF', false),
('Holy Wand',           'Varita sagrada para sanadores.',             'RARE',     false, NULL, '/img/holy_wand.png',    'SELF', false),
('Longbow',             'Arco largo de alta precisión.',              'UNCOMMON', false, NULL, '/img/longbow.png',      'SELF', false),
('Crossbow',            'Ballesta compacta y potente.',               'RARE',     false, NULL, '/img/crossbow.png',     'SELF', false),
('Blood Scythe',        'Guadaña que absorbe la vida enemiga.',       'EPIC',     false, NULL, '/img/blood_scythe.png', 'SELF', false),
('Stone Fist Gauntlet', 'Guantelete de piedra con tremendo impacto.', 'EPIC',     false, NULL, '/img/stone_fist.png',   'SELF', false),
('Twin Blades',         'Par de dagas para ataques dobles.',          'RARE',     false, NULL, '/img/twin_blades.png',  'SELF', false);

-- ---- CONSUMABLES (8) ----
INSERT INTO Item (name, description, quality, consumable, max_uses, image_thumb, target_type, is_aoe) VALUES
('Health Potion',      'Restaura 30% de HP.',                        'COMMON',   true, 1, '/img/health_pot.png',    'SELF',  false),
('Energy Elixir',      'Restaura 30% de energía.',                   'COMMON',   true, 1, '/img/energy_elixir.png', 'SELF',  false),
('Antidote',           'Elimina el estado Envenenado.',              'COMMON',   true, 1, '/img/antidote.png',      'SELF',  false),
('Rage Brew',          'Aplica Fortalecido lv1 al usuario.',         'UNCOMMON', true, 1, '/img/rage_brew.png',     'SELF',  false),
('Smoke Bomb',         'Aturde a todos los enemigos 1 turno.',       'UNCOMMON', true, 1, '/img/smoke_bomb.png',    'ENEMY', true),
('Greater Health Pot', 'Restaura 60% de HP.',                        'RARE',     true, 1, '/img/greater_hp.png',    'SELF',  false),
('Elixir of Speed',    'Aumenta velocidad un 30% durante 3 turnos.', 'UNCOMMON', true, 1, '/img/speed_elixir.png',  'SELF',  false),
('Phoenix Feather',    'Revive al usuario con el 50% de HP.',        'EPIC',     true, 1, '/img/phoenix.png',       'SELF',  false);

-- ---- NON-CONSUMABLES (8) ----
INSERT INTO Item (name, description, quality, consumable, max_uses, image_thumb, target_type, is_aoe) VALUES
('Lucky Charm',   'Amuleto que aumenta el crit_chance.',       'UNCOMMON', false, NULL, '/img/lucky_charm.png',   'SELF',  false),
('Stone Totem',   'Tótem que mejora la defensa pasivamente.',  'UNCOMMON', false, NULL, '/img/stone_totem.png',   'SELF',  false),
('Vampire Ring',  'Anillo que aplica Sangrado al atacar.',     'RARE',     false, NULL, '/img/vamp_ring.png',     'ENEMY', false),
('Poison Amulet', 'Amuleto que aplica Envenenado al atacar.',  'RARE',     false, NULL, '/img/poison_amulet.png', 'ENEMY', false),
('Battle Banner', 'Estandarte que da Fortalecido a aliados.',  'RARE',     false, NULL, '/img/banner.png',        'ALLY',  true),
('Speed Anklet',  'Tobillera que aumenta la velocidad.',       'UNCOMMON', false, NULL, '/img/anklet.png',        'SELF',  false),
('Accuracy Lens', 'Lente que mejora la precisión.',            'UNCOMMON', false, NULL, '/img/lens.png',          'SELF',  false),
('Crit Gem',      'Gema que mejora el daño crítico.',          'RARE',     false, NULL, '/img/crit_gem.png',      'SELF',  false);

-- =====================================================
-- 1️⃣1️⃣ LOOT ENTRIES
-- =====================================================

-- Goblin Berserker
INSERT INTO LootEntry (loot_table_id, item_id, drop_chance, min_quality, max_quality)
SELECT lt.id, i.id, 0.80, 'COMMON', 'COMMON' FROM LootTable lt JOIN Enemy en ON lt.enemy_id = en.entity_id JOIN Entity e ON en.entity_id = e.id JOIN Item i ON i.name = 'Health Potion' WHERE e.name = 'Goblin Berserker';
INSERT INTO LootEntry (loot_table_id, item_id, drop_chance, min_quality, max_quality)
SELECT lt.id, i.id, 0.60, 'COMMON', 'COMMON' FROM LootTable lt JOIN Enemy en ON lt.enemy_id = en.entity_id JOIN Entity e ON en.entity_id = e.id JOIN Item i ON i.name = 'Short Sword' WHERE e.name = 'Goblin Berserker';
INSERT INTO LootEntry (loot_table_id, item_id, drop_chance, min_quality, max_quality)
SELECT lt.id, i.id, 0.40, 'COMMON', 'UNCOMMON' FROM LootTable lt JOIN Enemy en ON lt.enemy_id = en.entity_id JOIN Entity e ON en.entity_id = e.id JOIN Item i ON i.name = 'Shadow Dagger' WHERE e.name = 'Goblin Berserker';
INSERT INTO LootEntry (loot_table_id, item_id, drop_chance, min_quality, max_quality)
SELECT lt.id, i.id, 0.30, 'COMMON', 'UNCOMMON' FROM LootTable lt JOIN Enemy en ON lt.enemy_id = en.entity_id JOIN Entity e ON en.entity_id = e.id JOIN Item i ON i.name = 'Iron Helm' WHERE e.name = 'Goblin Berserker';
INSERT INTO LootEntry (loot_table_id, item_id, drop_chance, min_quality, max_quality)
SELECT lt.id, i.id, 0.20, 'UNCOMMON', 'UNCOMMON' FROM LootTable lt JOIN Enemy en ON lt.enemy_id = en.entity_id JOIN Entity e ON en.entity_id = e.id JOIN Item i ON i.name = 'Rage Brew' WHERE e.name = 'Goblin Berserker';

-- Stone Golem
INSERT INTO LootEntry (loot_table_id, item_id, drop_chance, min_quality, max_quality)
SELECT lt.id, i.id, 0.85, 'COMMON', 'UNCOMMON' FROM LootTable lt JOIN Enemy en ON lt.enemy_id = en.entity_id JOIN Entity e ON en.entity_id = e.id JOIN Item i ON i.name = 'Iron Chestplate' WHERE e.name = 'Stone Golem';
INSERT INTO LootEntry (loot_table_id, item_id, drop_chance, min_quality, max_quality)
SELECT lt.id, i.id, 0.70, 'UNCOMMON', 'RARE' FROM LootTable lt JOIN Enemy en ON lt.enemy_id = en.entity_id JOIN Entity e ON en.entity_id = e.id JOIN Item i ON i.name = 'Stone Totem' WHERE e.name = 'Stone Golem';
INSERT INTO LootEntry (loot_table_id, item_id, drop_chance, min_quality, max_quality)
SELECT lt.id, i.id, 0.55, 'UNCOMMON', 'RARE' FROM LootTable lt JOIN Enemy en ON lt.enemy_id = en.entity_id JOIN Entity e ON en.entity_id = e.id JOIN Item i ON i.name = 'War Hammer' WHERE e.name = 'Stone Golem';
INSERT INTO LootEntry (loot_table_id, item_id, drop_chance, min_quality, max_quality)
SELECT lt.id, i.id, 0.40, 'RARE', 'EPIC' FROM LootTable lt JOIN Enemy en ON lt.enemy_id = en.entity_id JOIN Entity e ON en.entity_id = e.id JOIN Item i ON i.name = 'Golem Shell' WHERE e.name = 'Stone Golem';
INSERT INTO LootEntry (loot_table_id, item_id, drop_chance, min_quality, max_quality)
SELECT lt.id, i.id, 0.15, 'EPIC', 'EPIC' FROM LootTable lt JOIN Enemy en ON lt.enemy_id = en.entity_id JOIN Entity e ON en.entity_id = e.id JOIN Item i ON i.name = 'Stone Fist Gauntlet' WHERE e.name = 'Stone Golem';

-- Vampire Lord
INSERT INTO LootEntry (loot_table_id, item_id, drop_chance, min_quality, max_quality)
SELECT lt.id, i.id, 0.90, 'UNCOMMON', 'RARE' FROM LootTable lt JOIN Enemy en ON lt.enemy_id = en.entity_id JOIN Entity e ON en.entity_id = e.id JOIN Item i ON i.name = 'Vampire Ring' WHERE e.name = 'Vampire Lord';
INSERT INTO LootEntry (loot_table_id, item_id, drop_chance, min_quality, max_quality)
SELECT lt.id, i.id, 0.70, 'RARE', 'EPIC' FROM LootTable lt JOIN Enemy en ON lt.enemy_id = en.entity_id JOIN Entity e ON en.entity_id = e.id JOIN Item i ON i.name = 'Blood Scythe' WHERE e.name = 'Vampire Lord';
INSERT INTO LootEntry (loot_table_id, item_id, drop_chance, min_quality, max_quality)
SELECT lt.id, i.id, 0.50, 'RARE', 'EPIC' FROM LootTable lt JOIN Enemy en ON lt.enemy_id = en.entity_id JOIN Entity e ON en.entity_id = e.id JOIN Item i ON i.name = 'Vampiric Coat' WHERE e.name = 'Vampire Lord';
INSERT INTO LootEntry (loot_table_id, item_id, drop_chance, min_quality, max_quality)
SELECT lt.id, i.id, 0.35, 'RARE', 'EPIC' FROM LootTable lt JOIN Enemy en ON lt.enemy_id = en.entity_id JOIN Entity e ON en.entity_id = e.id JOIN Item i ON i.name = 'Vampire Mask' WHERE e.name = 'Vampire Lord';
INSERT INTO LootEntry (loot_table_id, item_id, drop_chance, min_quality, max_quality)
SELECT lt.id, i.id, 0.20, 'EPIC', 'EPIC' FROM LootTable lt JOIN Enemy en ON lt.enemy_id = en.entity_id JOIN Entity e ON en.entity_id = e.id JOIN Item i ON i.name = 'Phoenix Feather' WHERE e.name = 'Vampire Lord';
INSERT INTO LootEntry (loot_table_id, item_id, drop_chance, min_quality, max_quality)
SELECT lt.id, i.id, 0.60, 'COMMON', 'UNCOMMON' FROM LootTable lt JOIN Enemy en ON lt.enemy_id = en.entity_id JOIN Entity e ON en.entity_id = e.id JOIN Item i ON i.name = 'Greater Health Pot' WHERE e.name = 'Vampire Lord';

-- Dark Assassin
INSERT INTO LootEntry (loot_table_id, item_id, drop_chance, min_quality, max_quality)
SELECT lt.id, i.id, 0.80, 'UNCOMMON', 'RARE' FROM LootTable lt JOIN Enemy en ON lt.enemy_id = en.entity_id JOIN Entity e ON en.entity_id = e.id JOIN Item i ON i.name = 'Poison Blade' WHERE e.name = 'Dark Assassin';
INSERT INTO LootEntry (loot_table_id, item_id, drop_chance, min_quality, max_quality)
SELECT lt.id, i.id, 0.65, 'UNCOMMON', 'RARE' FROM LootTable lt JOIN Enemy en ON lt.enemy_id = en.entity_id JOIN Entity e ON en.entity_id = e.id JOIN Item i ON i.name = 'Shadow Cloak' WHERE e.name = 'Dark Assassin';
INSERT INTO LootEntry (loot_table_id, item_id, drop_chance, min_quality, max_quality)
SELECT lt.id, i.id, 0.50, 'UNCOMMON', 'RARE' FROM LootTable lt JOIN Enemy en ON lt.enemy_id = en.entity_id JOIN Entity e ON en.entity_id = e.id JOIN Item i ON i.name = 'Twin Blades' WHERE e.name = 'Dark Assassin';
INSERT INTO LootEntry (loot_table_id, item_id, drop_chance, min_quality, max_quality)
SELECT lt.id, i.id, 0.40, 'UNCOMMON', 'RARE' FROM LootTable lt JOIN Enemy en ON lt.enemy_id = en.entity_id JOIN Entity e ON en.entity_id = e.id JOIN Item i ON i.name = 'Shadow Hood' WHERE e.name = 'Dark Assassin';
INSERT INTO LootEntry (loot_table_id, item_id, drop_chance, min_quality, max_quality)
SELECT lt.id, i.id, 0.30, 'RARE', 'RARE' FROM LootTable lt JOIN Enemy en ON lt.enemy_id = en.entity_id JOIN Entity e ON en.entity_id = e.id JOIN Item i ON i.name = 'Poison Amulet' WHERE e.name = 'Dark Assassin';
INSERT INTO LootEntry (loot_table_id, item_id, drop_chance, min_quality, max_quality)
SELECT lt.id, i.id, 0.55, 'COMMON', 'UNCOMMON' FROM LootTable lt JOIN Enemy en ON lt.enemy_id = en.entity_id JOIN Entity e ON en.entity_id = e.id JOIN Item i ON i.name = 'Antidote' WHERE e.name = 'Dark Assassin';