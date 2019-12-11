.PHONY : all clean mod dist

NAME = DraggableAltimeter
LIB = libs
CONF = build/PluginData/$(NAME)

all: mod
mod : build/$(NAME).dll build/README.md build/LICENSE.md build/CHANGES.md $(CONF)/$(NAME).cfg

build/%.dll : src/%.cs
	@mkdir -p $(@D)
	mcs $< \
		-target:library \
		-out:$@ \
		-lib:$(LIB) \
		-reference:Assembly-CSharp.dll \
		-reference:UnityEngine.dll \
		-reference:UnityEngine.CoreModule.dll \
		-reference:UnityEngine.UI.dll

build/%.md : %.md
	@mkdir -p $(@D)
	cp -f $< $@

$(CONF)/%.cfg : src/%.cfg
	@mkdir -p $(@D)
	cp -f $< $@

dist : mod
	@mkdir -p dist
	ln -sfn ../build dist/$(NAME) && \
	cd dist && \
	zip -FSr $(NAME).zip $(NAME)

clean : 
	-rm -rf build
	-rm -rf dist
