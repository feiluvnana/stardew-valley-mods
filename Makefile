MODS := BetterChest BetterForge BetterFurniture BetterIndustry BetterMap BetterQOL ExtendedDesertFestival

VERBS := all help build rebuild clean

SELECTED := $(strip $(filter-out $(VERBS),$(MAKECMDGOALS)))

ifneq ($(filter-out $(MODS),$(SELECTED)),)
$(error Unknown mod(s): '$(filter-out $(MODS),$(SELECTED))'. Available: $(MODS))
endif

ifeq ($(SELECTED),)
BUILD_GOALS := $(addsuffix .build,$(MODS))
CLEAN_GOALS := $(addsuffix .clean,$(MODS))
else
BUILD_GOALS := $(addsuffix .build,$(SELECTED))
CLEAN_GOALS := $(addsuffix .clean,$(SELECTED))
endif

.DEFAULT_GOAL := help
.NOTPARALLEL:

.PHONY: all help build rebuild clean $(MODS)

## make            -> build all
## make build      -> build all
## make build NAME -> build one mod
## make rebuild [NAME], make clean [NAME], make NAME
help:
	@echo Usage:
	@echo   make                  Build all mods
	@echo   make build [NAME]     Build all, or just NAME
	@echo   make rebuild [NAME]   Clean then build all, or just NAME
	@echo   make clean [NAME]     Clean all, or just NAME
	@echo   make NAME             Shortcut for: make build NAME
	@echo Mods: $(MODS)

all: build

build: $(BUILD_GOALS)

clean: $(CLEAN_GOALS)

rebuild: $(CLEAN_GOALS) $(BUILD_GOALS)

$(MODS): %: %.build

define MOD_RULES
.PHONY: $(1).build $(1).clean
$(1).build:
	dotnet build "$(1)/$(1).csproj"

$(1).clean:
	dotnet clean "$(1)/$(1).csproj"
endef
$(foreach m,$(MODS),$(eval $(call MOD_RULES,$(m))))
