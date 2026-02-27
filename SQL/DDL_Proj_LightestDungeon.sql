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
  `max_uses` int,
  `image_thumb` varchar(500) NOT NULL
);

CREATE TABLE `Effect` (
  `id` int PRIMARY KEY AUTO_INCREMENT,
  `stat_id` int,
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