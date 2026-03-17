-- =====================================================
-- DROP TRIGGERS
-- =====================================================
DROP TRIGGER IF EXISTS trg_entity_check_stats;
DROP TRIGGER IF EXISTS trg_entity_update_stats;
DROP TRIGGER IF EXISTS trg_enemy_create_loot_table;
DROP TRIGGER IF EXISTS trg_effect_validate;
DROP TRIGGER IF EXISTS trg_item_consumable_check;

-- =====================================================
-- DROP TABLES (en orden inverso por foreign keys)
-- =====================================================
DROP TABLE IF EXISTS `SkillEffect`;
DROP TABLE IF EXISTS `ItemEffect`;
DROP TABLE IF EXISTS `LootEntry`;
DROP TABLE IF EXISTS `LootTable`;
DROP TABLE IF EXISTS `EntitySkill`;
DROP TABLE IF EXISTS `Effect`;
DROP TABLE IF EXISTS `Player`;
DROP TABLE IF EXISTS `Enemy`;
DROP TABLE IF EXISTS `Entity`;
DROP TABLE IF EXISTS `Skill`;
DROP TABLE IF EXISTS `Item`;
DROP TABLE IF EXISTS `Statistic`;
DROP TABLE IF EXISTS `Status`;

CREATE TABLE `Entity` (
  `id` int PRIMARY KEY AUTO_INCREMENT,
  `name` varchar(50) NOT NULL,
  `level` int NOT NULL,
  `hp` int NOT NULL,
  `hp_max` int NOT NULL,
  `energy` int NOT NULL,
  `energy_max` int NOT NULL,
  `attack` int NOT NULL,
  `defense` int NOT NULL,
  `speed` int NOT NULL,
  `crit_chance` float NOT NULL,
  `crit_damage` float NOT NULL,
  `accuracy_multiplier` float NOT NULL,
  `image_thumb` varchar(500) NOT NULL,
  `image_full` varchar(500) NOT NULL,
  `description` varchar(500)
);

CREATE TABLE `Player` (
  `entity_id` int PRIMARY KEY,
  `xp_points` int NOT NULL,
  `skill_points` int NOT NULL
);

CREATE TABLE `Enemy` (
  `entity_id` int PRIMARY KEY,
  `passiveId` int NOT NULL
);

CREATE TABLE `Skill` (
  `id` int PRIMARY KEY AUTO_INCREMENT,
  `name` varchar(50) NOT NULL,
  `description` varchar(500),
  `energy_cost` int NOT NULL,
  `accuracy` float NOT NULL,
  `hits` int NOT NULL,
  `target_type` varchar(20) NOT NULL,
  `is_aoe` boolean NOT NULL,
  `is_passive` boolean NOT NULL,
  `image_thumb` varchar(500) NOT NULL
);

CREATE TABLE `EntitySkill` (
  `entity_id` int,
  `skill_id` int,
  PRIMARY KEY (`entity_id`, `skill_id`)
);

CREATE TABLE `Item` (
  `id` int PRIMARY KEY AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  `description` varchar(500),
  `quality` varchar(20) NOT NULL,
  `consumable` boolean NOT NULL,
  `target_type` varchar(20) NOT NULL,
  `is_aoe` boolean NOT NULL,
  `max_uses` int,
  `image_thumb` varchar(500) NOT NULL
);

CREATE TABLE `Effect` (
  `id` int PRIMARY KEY AUTO_INCREMENT,
  `stat_id` int,
  `min_flat_power` int,
  `max_flat_power` int,
  `stat_multiplier` float,
  `status_id` int,
  `effect_level` int,
  `probability` float NOT NULL,
  `duration_turns` int NOT NULL
);

CREATE TABLE `ItemEffect` (
  `item_id` int,
  `effect_id` int,
  PRIMARY KEY (`item_id`, `effect_id`)
);

CREATE TABLE `SkillEffect` (
  `skill_id` int,
  `effect_id` int,
  PRIMARY KEY (`skill_id`, `effect_id`)
);

CREATE TABLE `Statistic` (
  `id` int PRIMARY KEY AUTO_INCREMENT,
  `name` varchar(50) NOT NULL
);

CREATE TABLE `Status` (
  `id` int PRIMARY KEY AUTO_INCREMENT,
  `name` varchar(50) NOT NULL,
  `max_level` int NOT NULL,
  `description` varchar(500),
  `scaling_formula` varchar(250)
);

CREATE TABLE `LootTable` (
  `id` int PRIMARY KEY AUTO_INCREMENT,
  `enemy_id` int NOT NULL
);

CREATE TABLE `LootEntry` (
  `loot_table_id` int NOT NULL,
  `num` int PRIMARY KEY AUTO_INCREMENT,
  `item_id` int NOT NULL,
  `drop_chance` float NOT NULL,
  `min_quality` varchar(20) NOT NULL,
  `max_quality` varchar(20) NOT NULL
);

ALTER TABLE `Player` ADD FOREIGN KEY (`entity_id`) REFERENCES `Entity` (`id`);

ALTER TABLE `Enemy` ADD FOREIGN KEY (`entity_id`) REFERENCES `Entity` (`id`);

ALTER TABLE `ItemEffect` ADD FOREIGN KEY (`item_id`) REFERENCES `Item` (`id`);

ALTER TABLE `ItemEffect` ADD FOREIGN KEY (`effect_id`) REFERENCES `Effect` (`id`);

ALTER TABLE `LootTable` ADD FOREIGN KEY (`enemy_id`) REFERENCES `Enemy` (`entity_id`);

ALTER TABLE `LootEntry` ADD FOREIGN KEY (`loot_table_id`) REFERENCES `LootTable` (`id`);

ALTER TABLE `LootEntry` ADD FOREIGN KEY (`item_id`) REFERENCES `Item` (`id`);

ALTER TABLE `EntitySkill` ADD FOREIGN KEY (`entity_id`) REFERENCES `Entity` (`id`);

ALTER TABLE `EntitySkill` ADD FOREIGN KEY (`skill_id`) REFERENCES `Skill` (`id`);

ALTER TABLE `SkillEffect` ADD FOREIGN KEY (`skill_id`) REFERENCES `Skill` (`id`);

ALTER TABLE `SkillEffect` ADD FOREIGN KEY (`effect_id`) REFERENCES `Effect` (`id`);

ALTER TABLE `Effect` ADD FOREIGN KEY (`stat_id`) REFERENCES `Statistic` (`id`);

ALTER TABLE `Effect` ADD FOREIGN KEY (`status_id`) REFERENCES `Status` (`id`);

-- =====================================================
-- TRIGGERS
-- =====================================================

DELIMITER $$

-- TRIGGER 1: Sincroniza hp_max al crear una Entity.
-- Garantiza que hp_max >= hp y energy_max >= energy en todo momento.
-- Evita inconsistencias en el momento de inserción de datos.
CREATE TRIGGER trg_entity_check_stats
BEFORE INSERT ON Entity
FOR EACH ROW
BEGIN
    IF NEW.hp > NEW.hp_max THEN
        SET NEW.hp_max = NEW.hp;
    END IF;
    IF NEW.energy > NEW.energy_max THEN
        SET NEW.energy_max = NEW.energy;
    END IF;
END$$

-- TRIGGER 2: Igual que el anterior pero en UPDATE.
-- Si se modifica la entidad, hp y energy nunca superan sus máximos.
CREATE TRIGGER trg_entity_update_stats
BEFORE UPDATE ON Entity
FOR EACH ROW
BEGIN
    IF NEW.hp > NEW.hp_max THEN
        SET NEW.hp = NEW.hp_max;
    END IF;
    IF NEW.energy > NEW.energy_max THEN
        SET NEW.energy = NEW.energy_max;
    END IF;
    IF NEW.hp < 0 THEN
        SET NEW.hp = 0;
    END IF;
    IF NEW.energy < 0 THEN
        SET NEW.energy = 0;
    END IF;
END$$

-- TRIGGER 3: Al insertar un Enemy, crea automáticamente su LootTable.
-- Evita olvidar crear la LootTable manualmente cada vez que se añade un enemigo.
CREATE TRIGGER trg_enemy_create_loot_table
AFTER INSERT ON Enemy
FOR EACH ROW
BEGIN
    INSERT INTO LootTable (enemy_id) VALUES (NEW.entity_id);
END$$

-- TRIGGER 4: Valida que un Effect no tenga stat_id y status_id a la vez nulos.
-- Al menos uno debe estar definido para que el efecto tenga sentido funcional.
CREATE TRIGGER trg_effect_validate
BEFORE INSERT ON Effect
FOR EACH ROW
BEGIN
    IF NEW.stat_id IS NULL AND NEW.status_id IS NULL THEN
        -- Permitimos el caso del Antidote (level 0 limpia estado) sin lanzar error
        -- pero sí bloqueamos efectos completamente vacíos con probability != 1.0
        IF NEW.probability != 1.0 THEN
            SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Effect debe tener stat_id o status_id definido.';
        END IF;
    END IF;
    IF NEW.probability < 0.0 OR NEW.probability > 1.0 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Effect.probability debe estar entre 0.0 y 1.0.';
    END IF;
END$$

-- TRIGGER 5: Fuerza max_uses >= 1 en consumibles al insertar un Item.
-- Evita consumibles sin usos definidos que nunca podrían usarse.
CREATE TRIGGER trg_item_consumable_check
BEFORE INSERT ON Item
FOR EACH ROW
BEGIN
    IF NEW.consumable = true AND (NEW.max_uses IS NULL OR NEW.max_uses < 1) THEN
        SET NEW.max_uses = 1;
    END IF;
END$$

DELIMITER ;