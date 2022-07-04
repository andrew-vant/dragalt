.PHONY : all clean mod dist meta release
.DELETE_ON_ERROR :

NAME = DraggableAltimeter
LIB = libs
CONF = build/PluginData/$(NAME)

all: mod
mod : build/$(NAME).dll build/README.md build/LICENSE.md build/CHANGES.md $(CONF)/$(NAME).cfg
meta: build/$(NAME).version
release: clean meta dist

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

# NOTE: meta.py returns nonzero if the build should not be released
# (e.g no version tag or dirty tree). Make will delete the file in that
# case per .DELETE_ON_ERROR above, to try to prevent zips with bogus version
# information from getting into the wild. To force it to keep the file
# (e.g. for debugging), use `make -i meta`
build/$(NAME).version : meta/meta.py meta/meta.yaml meta/version.yaml.jinja
	@mkdir -p $(@D)
	$< > $@

# NOTE: the metadata file is intentionally not a prereq here, to allow
# test zips to be built. The meta file will get into the zip only if
# it's intentionally built first.
dist : mod
	@mkdir -p dist
	ln -sfn ../build dist/$(NAME) && \
	cd dist && \
	zip -FSr $(NAME).zip $(NAME)

clean : 
	-rm -rf build
	-rm -rf dist
