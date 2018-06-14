.PHONY: all
.PHONY: documentation
.PHONY: clean

SHELL:=/bin/bash

ISP_PREFIX ?= /opt/isp/

PROJECTS := ValidatorPlugin

CLEAN_PROJECTS := $(patsubst %,clean-%,$(PROJECTS))
INSTALL_PROJECTS := $(patsubst %,install-%,$(PROJECTS))

.PHONY: $(PROJECTS)
.PHONY: $(CLEAN_PROJECTS)
.PHONY: $(INSTALL_PROJECTS)

all: $(PROJECTS)

$(PROJECTS):
	$(MAKE) -C $@

install: $(INSTALL_PROJECTS)

$(INSTALL_PROJECTS):
	$(MAKE) -C $(@:install-%=%) install

$(CLEAN_PROJECTS):
	$(MAKE) -C $(@:clean-%=%) clean

clean: $(CLEAN_PROJECTS)
