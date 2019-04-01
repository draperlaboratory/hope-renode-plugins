.PHONY: all
.PHONY: install
.PHONY: clean


ISP_PREFIX ?= $(HOME)/.local/isp/

all: bin/Debug/ValidatorPlugin.dll

bin/Debug/ValidatorPlugin.dll: *.cs
	xbuild

install: all
	install -d ../../renode/output/bin/Release/
	install -p -t ../../renode/output/bin/Release/ bin/Debug/ValidatorPlugin.dll
	install -d $(ISP_PREFIX)/renode/bin/
	install -p -t $(ISP_PREFIX)/renode/bin/ bin/Debug/ValidatorPlugin.dll

clean:
	$(RM) -r bin obj
	$(RM) $(ISP_PREFIX)/renode/bin/ValidatorPlugin.dll
	$(RM) ../../renode/output/bin/Release/ValidatorPlugin.dll
